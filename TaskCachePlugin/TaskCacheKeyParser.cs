using Registry;
using Registry.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;

namespace TaskCachePlugin
{
    public static class TimestampUtil
    {
        private static readonly DateTime _null = DateTime.Parse("1970-01-01T00:00:00");

        public static DateTime Null { get => _null; }
    }

    public static class TaskCacheKeyParser
    {
        public static TaskCacheKey[] Parse(RegistryHive hive)
        {
            RegistryKey taskCache = hive.GetKey("Microsoft\\Windows NT\\CurrentVersion\\Schedule\\TaskCache");
            return Parse(taskCache);
        }

        public static TaskCacheKey[] Parse(RegistryKey taskCache)
        {
            if (!taskCache.KeyPath.Contains("Microsoft\\Windows NT\\CurrentVersion\\Schedule\\TaskCache"))
                throw new ArgumentException($"Isn't TaskCache path: {taskCache.KeyPath}");

            Dictionary<Guid, TaskCacheKey> result = new Dictionary<Guid, TaskCacheKey>();
            RegistryKey tasksKey = taskCache.SubKeys.SingleOrDefault(t => t.KeyName == "Tasks");
            RegistryKey treeKey = taskCache.SubKeys.SingleOrDefault(t => t.KeyName == "Tree");

            int total = tasksKey.SubKeys.Count;
            foreach (var guidKey in tasksKey.SubKeys)
            {
                try
                {
                    TaskCacheKey task = new TaskCacheKey();
                    task.Id = new Guid(guidKey.KeyName);
                    result.Add(task.Id, task);
                    var tempAuthor = guidKey.Values.SingleOrDefault(t => t.ValueName == "Author")?.ValueData;
                    task.Author = (tempAuthor != null && Regex.IsMatch(tempAuthor, @"^([\w._-]+)\\([\w._-]+)\$?$")) ? tempAuthor : null;
                    task.DynamicInfo = new DynamicInfoKeyValue(guidKey.Values.SingleOrDefault(t => t.ValueName == "DynamicInfo")?.ValueDataRaw);
                    task.Hash = guidKey.Values.SingleOrDefault(t => t.ValueName == "Hash")?.ValueData.Replace("-", string.Empty) ?? string.Empty;
                    task.Path = guidKey.Values.SingleOrDefault(t => t.ValueName == "Path")?.ValueData ?? string.Empty;
                    task.SetDataFromActionsKey(guidKey.Values.SingleOrDefault(t => t.ValueName == "Actions")?.ValueDataRaw);
                }
                catch { }
            }

            RegistryKey tree = taskCache.SubKeys.SingleOrDefault(t => t.KeyName == "Tree");
            SetDataFromTreeKey(result, tree, ParseSecurityDescriptor(tree.Values.SingleOrDefault(t => t.ValueName == "SD")?.ValueDataRaw));

            return result.Values.ToArray();
        }

        private static string ParseSecurityDescriptor(byte[] raw)
        {
            if (raw is null || raw.Length == 0) return "[alert: sd is missing]";

            try
            {
                var securityDescriptor = new RawSecurityDescriptor(raw, 0);
                var owner = (securityDescriptor.Owner != null) ? securityDescriptor.GetSddlForm(AccessControlSections.Owner) : "alert: owner is null";
                var group = (securityDescriptor.Group != null) ? securityDescriptor.GetSddlForm(AccessControlSections.Group) : "alert: group is null";
                return $"[({owner})({group})]";
            }
            catch
            {
                return "[alert: invalid sd]";
            }
        }

        private static void SetDataFromTreeKey(Dictionary<Guid, TaskCacheKey> result, RegistryKey tree, string previousPathSD)
        {
            foreach (var subKey in tree.SubKeys)
            {
                try
                {
                    var currentNodeSD = ParseSecurityDescriptor(subKey.Values.SingleOrDefault(t => t.ValueName == "SD")?.ValueDataRaw);
                    var pathSD = previousPathSD.Contains(currentNodeSD) ? previousPathSD : string.Join(" ", previousPathSD, currentNodeSD);
                    if (subKey.SubKeys.Count > 0)
                    {
                        SetDataFromTreeKey(result, subKey, pathSD);
                    }

                    var id = subKey.Values.SingleOrDefault(t => t.ValueName == "Id");
                    if (id != null && Guid.TryParse(id.ValueData, out var guid) && result.TryGetValue(guid, out var task))
                    {
                        var indexStr = subKey.Values.SingleOrDefault(t => t.ValueName == "Index")?.ValueData;
                        if (int.TryParse(indexStr, out var index)) task.Index = index;
                        task.TreeSecurityDescriptor = pathSD;
                        task.TreeKeyLastWrite = subKey.LastWriteTime?.UtcDateTime ?? TimestampUtil.Null;
                    }
                }
                catch { }
            }
        }

        private static void SetDataFromActionsKey(this TaskCacheKey task, byte[] raw)
        {
            if (raw is null) return;

            using (Stream stream = new MemoryStream(raw))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                task.Actions.Version = reader.ReadInt16();
                if (task.Actions.Version >= 2) task.Actions.Context = Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));

                try
                {
                    while (reader.PeekChar() != -1)
                    {
                        ActionBodyType type;
                        try
                        {
                            type = (ActionBodyType)reader.ReadInt16();
                        }
                        catch
                        {
                            type = ActionBodyType.Unknown;
                        }

                        switch (type)
                        {
                            case ActionBodyType.Execution:
                                {
                                    var body = new ActionBodyExecution();
                                    body.Id = Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
                                    body.Path = Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
                                    body.Arguments = Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
                                    body.WorkingDirectory = Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
                                    if (task.Actions.Version >= 3) body.Flags = reader.ReadInt16();
                                    task.Actions.Body.Add(body);
                                    break;
                                }

                            case ActionBodyType.ComHandler:
                                {
                                    var body = new ActionBodyComHandler();
                                    body.Id = Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
                                    body.CLSID = $"{{{new Guid(reader.ReadBytes(16)).ToString().ToUpper()}}}";
                                    body.Data = Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
                                    task.Actions.Body.Add(body);
                                    break;
                                }

                            default:
                            case ActionBodyType.Unknown:
                                {
                                    throw new NotImplementedException("Unknown or not implemented TaskCacheType");
                                }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var body = new ActionBodyUnknown()
                    {
                        Data = $"Error: {ex.GetType()}: {ex.Message} (raw: {BitConverter.ToString(raw)})"
                    };
                    task.Actions.Body.Add(body);
                }
            }
        }

        public static string GetActionsAsString(this TaskCacheKey task)
        {
            switch (task.Actions.Body.Count)
            {
                case 1:
                    {
                        return task.Actions.Body[0].GetMessage();
                    }
                default:
                    {
                        return $"{string.Join(" | ", task.Actions.Body.Select(x => $"[{x.GetMessage()}]"))}";
                    }
            }
        }

        public static bool Check(this TaskCacheKey task)
        {
            if (task.Index == 0 || task.TreeSecurityDescriptor.Contains("alert"))
            {
                return true;
            }

            return false;
        }
    }
}