namespace RszFile
{
    public class UserFile
    {

        public struct Header {
            public uint magic;
            public uint resourceCount;
            public uint userdataCount;
            public uint infoCount;
            public ulong resourceInfoOffset;
            public ulong userdataInfoOffset;
            public ulong dataOffset;
        };

        public struct ResourceInfo {
            public ulong pathOffset;
            // public string resourcePath;
        };

        public struct UserdataInfo {
            public uint typeId;
            public uint CRC;
            public ulong pathOffset;
            // public string userdataPath;
        };
    }
}
