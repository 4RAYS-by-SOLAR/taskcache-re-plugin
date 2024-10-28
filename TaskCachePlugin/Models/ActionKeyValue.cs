using System.Collections.Generic;
using System.Text;

namespace TaskCachePlugin
{
    public enum ActionBodyType
    {
        Unknown = 0,
        Execution = 0x6666,
        ComHandler = 0x7777,
        Email = 0x8888,
        MessageBox = 0x9999
    }

    public interface IActionBody
    {
        string GetMessage();
        ActionBodyType Type { get; }
    }

    public enum TriggerType
    {
        Boot = 1,
        Logon = 2,
        Plain = 3,
        Maintenance = 4
    }

    public class ActionKeyValue
    {
        public int Version { get; set; } = -1;
        public string Context { get; set; } = string.Empty;
        public List<IActionBody> Body { get; set; } = new List<IActionBody>();
    }

    public class ActionBodyExecution : IActionBody
    {
        public ActionBodyType Type { get => ActionBodyType.Execution; }

        public string Id { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public string Arguments { get; set; } = string.Empty;

        public string WorkingDirectory { get; set; } = string.Empty;

        public short Flags { get; set; } = 0;

        public string GetMessage()
        {
            List<string> row = new List<string>();
            if (Path?.Length > 0) row.Add(Path);
            if (Arguments?.Length > 0) row.Add(Arguments);
            if (WorkingDirectory?.Length > 0 || Id?.Length > 0)
            {
                StringBuilder temp = new StringBuilder();
                temp.Append('(');
                if (WorkingDirectory?.Length > 0)
                    temp.Append($"directory: {WorkingDirectory}");

                if (Id?.Length > 0)
                {
                    if (WorkingDirectory?.Length > 0)
                        temp.Append(", ");
                    temp.Append($"id: {Id}");
                }
                temp.Append(')');
                row.Add(temp.ToString());
            }
            return string.Join(" ", row);
        }
    }

    public class ActionBodyComHandler : IActionBody
    {
        public ActionBodyType Type { get => ActionBodyType.ComHandler; }

        public string Id { get; set; } = string.Empty;

        public string CLSID { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public string GetMessage()
        {
            List<string> row = new List<string>();
            if (CLSID?.Length > 0) row.Add($"CLSID: {CLSID}");
            if (Data?.Length > 0) row.Add(Data);
            return string.Join(" ", row);
        }
    }

    public class ActionBodyUnknown : IActionBody
    {
        public ActionBodyType Type { get => ActionBodyType.Unknown; }

        public string Data { get; set; } = string.Empty;

        public string GetMessage() => Data?.Length > 0 ? Data : string.Empty;
    }
}