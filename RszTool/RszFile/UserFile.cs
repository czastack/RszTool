namespace RszTool
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

        // ResourceInfo
        // UserdataInfo

        public StructModel<Header> dataHeader = new();
        public ResourceInfo dataResourceInfo = new();
        public UserdataInfo dataUserdataInfo = new();


        public UserFile()
        {
        }

        public bool Read(FileHandler handler)
        {
            if (!dataHeader.Read(handler)) return false;
            if (!dataResourceInfo.Read(handler)) return false;
            if (!dataUserdataInfo.Read(handler)) return false;
            return true;
        }

        public bool Write(FileHandler handler)
        {
            if (!dataHeader.Write(handler)) return false;
            if (!dataResourceInfo.Write(handler)) return false;
            if (!dataUserdataInfo.Write(handler)) return false;
            return true;
        }
    }
}
