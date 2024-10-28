using RegistryPluginBase.Interfaces;
using System;
using System.Linq;

namespace TaskCachePlugin
{
    public class ValuesOut : IValueOut
    {
        private TaskCacheKey _key;

        public string KeyName { get => $"{{{_key.Id.ToString().ToUpper()}}}"; }
        public string Path { get => _key.Path; }
        public string Actions { get => _key.GetActionsAsString(); }
        public string ActionsType { get => string.Join(" | ", _key.Actions.Body.Select(x => x.Type.ToString())); }

        public DateTimeOffset TreeKeyLastWrite { get => _key.TreeKeyLastWrite; }
        public DateTimeOffset CreatedOn { get => _key.DynamicInfo.CreatedOn; }
        public DateTimeOffset? LastStart { get => _key.DynamicInfo.LastStart; }
        public DateTimeOffset? LastStop { get => _key.DynamicInfo.LastStop; }

        public bool Alert { get => _key.Check(); }

        public int Index { get => _key.Index; }
        public string TreeSD { get => _key.TreeSecurityDescriptor; }

        public ValuesOut(TaskCacheKey key) => _key = key;

        public string BatchKeyPath { get; set; } = string.Empty;
        public string BatchValueName { get; set; } = string.Empty;
        public string BatchValueData1 => $"Created on: {CreatedOn.ToUniversalTime():yyyy-MM-dd HH:mm:ss.fffffff}";
        public string BatchValueData2 => $"Last start: {LastStart?.ToUniversalTime():yyyy-MM-dd HH:mm:ss.fffffff}, Last stop: {LastStop?.ToUniversalTime():yyyy-MM-dd HH:mm:ss.fffffff}";
        public string BatchValueData3 => $"Path: {Path}";

        public override string ToString()
        {
            return $"{BatchValueData1} {BatchValueData2} {BatchValueData3}";
        }
    }
}