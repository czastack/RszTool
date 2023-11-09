namespace RszTool
{
    public class UserFile : BaseRszFile
    {
        public struct HeaderStruct {
            public uint magic;
            public int resourceCount;
            public int userdataCount;
            public int infoCount;
            public long resourceInfoOffset;
            public long userdataInfoOffset;
            public long dataOffset;
        };

        public StructModel<HeaderStruct> Header = new();
        public List<ResourceInfo> ResourceInfoList = new();
        public List<UserdataInfo> UserdataInfoList = new();
        public RSZFile? RSZ { get; private set; }


        public UserFile(RszFileOption option, FileHandler fileHandler) : base(option, fileHandler)
        {
        }

        protected override bool DoRead()
        {
            var handler = FileHandler;
            if (!Header.Read(handler)) return false;
            handler.Seek(Header.Data.dataOffset);
            for (int i = 0; i < Header.Data.resourceCount; i++)
            {
                ResourceInfo resourceInfo = new();
                resourceInfo.Read(handler);
                ResourceInfoList.Add(resourceInfo);
            }

            handler.Seek(Header.Data.userdataInfoOffset);
            for (int i = 0; i < Header.Data.userdataCount; i++)
            {
                UserdataInfo userdataInfo = new();
                userdataInfo.Read(handler);
                UserdataInfoList.Add(userdataInfo);
            }

            RSZ = new RSZFile(Option, FileHandler.WithOffset(Header.Data.dataOffset));
            RSZ.Read();
            return true;
        }

        protected override bool DoWrite()
        {
            var handler = FileHandler;

            handler.Seek(Header.Size);
            handler.Align(16);
            Header.Data.resourceInfoOffset = handler.Tell();
            ResourceInfoList.Write(handler);

            handler.Align(16);
            Header.Data.userdataInfoOffset = handler.Tell();
            UserdataInfoList.Write(handler);

            handler.FlushStringToWrite();

            Header.Data.dataOffset = handler.Tell();
            RSZ!.Write(Header.Data.dataOffset);

            handler.Seek(0);
            Header.Data.resourceCount = ResourceInfoList.Count;
            Header.Data.userdataCount = UserdataInfoList.Count;
            Header.Write(handler);

            return true;
        }

        public bool Save(string filePath)
        {
            return false;
        }
    }
}
