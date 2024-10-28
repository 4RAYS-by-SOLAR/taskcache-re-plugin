using System;

namespace TaskCachePlugin
{
    public class TaskCacheKey
    {
        public DateTime TreeKeyLastWrite { get; set; } = TimestampUtil.Null;
        public Guid Id { get; set; }

        //from TaskCache\Tree\Path
        public int Index { get; set; } = -1;
        public string TreeSecurityDescriptor { get; set; } = string.Empty;

        //from TaskCache\Tasks\{GUID}
        public ActionKeyValue Actions { get; set; } = new ActionKeyValue();
        public string ActionSecurityDescriptor { get; set; } = null;

        public string Author { get; set; } = null;
        public DynamicInfoKeyValue DynamicInfo { get; set; }
        public string Hash { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}