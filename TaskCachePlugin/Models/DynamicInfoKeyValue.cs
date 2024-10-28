using System;

namespace TaskCachePlugin
{
    public class DynamicInfoKeyValue
    {
        public int Version { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset LastStart { get; set; }
        public DateTimeOffset LastStop { get; set; }
        public int ActionResult { get; set; }
        public int State { get; set; }

        public DynamicInfoKeyValue()
        {
            Version = 3;
            CreatedOn = TimestampUtil.Null;
            LastStart = TimestampUtil.Null;
            LastStop = TimestampUtil.Null;
            ActionResult = 0;
            State = 0;
        }

        public DynamicInfoKeyValue(byte[] bytes) : this()
        {
            if (bytes is null) return;
            //bytes.Length = 0x1c OR 0x24

            Version = BitConverter.ToInt32(bytes, 0);
            long createdRaw = BitConverter.ToInt64(bytes, 0x4);
            if (createdRaw != 0)
                CreatedOn = DateTimeOffset.FromFileTime(createdRaw).ToUniversalTime();

            long lastStartRaw = BitConverter.ToInt64(bytes, 0xc);
            if (lastStartRaw != 0)
                LastStart = DateTimeOffset.FromFileTime(lastStartRaw).ToUniversalTime();

            State = BitConverter.ToInt32(bytes, 0x14);
            ActionResult = BitConverter.ToInt32(bytes, 0x18);

            if (bytes.Length != 0x24) return;

            long lastStopRaw = BitConverter.ToInt64(bytes, 0x1c);
            if (lastStopRaw != 0)
                LastStop = DateTimeOffset.FromFileTime(lastStopRaw).ToUniversalTime();
        }
    }
}