namespace RszTool
{
    public class RSZFile
    {

        public struct Header
        {
            public uint magic;
            public uint version;
            public uint objectCount;
            public uint instanceCount;
            public ulong userdataCount;
            public ulong instanceOffset;
            public ulong dataOffset;
            public ulong userdataOffset;
        }

        public struct ObjectTable
        {
            public uint instanceId;
        }

        public struct InstanceInfo
        {
            public uint typeId;
            public uint CRC;
        }

        public struct RSZUserDataInfo
        {
            public uint instanceId;
            public uint typeId;
            public ulong pathOffset;
            // public string path;
        }

        public struct RSZUserDataInfo_TDB_LE_67
        {
            public uint instanceId;
            public uint typeId;
            public uint jsonPathHash;
            public uint dataSize;
            public ulong RSZOffset;
        }
    }
}
