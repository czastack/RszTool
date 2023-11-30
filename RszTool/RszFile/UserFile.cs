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

        public StructModel<HeaderStruct> Header { get; } = new();
        public List<ResourceInfo> ResourceInfoList { get; } = new();
        public List<UserdataInfo> UserdataInfoList { get; } = new();
        public RSZFile? RSZ { get; private set; }


        public UserFile(RszFileOption option, FileHandler fileHandler) : base(option, fileHandler)
        {
            if (fileHandler.FilePath != null)
            {
                RszUtils.CheckFileExtension(fileHandler.FilePath, Extension2, GetVersionExt());
            }
        }

        public const uint Magic = 0x525355;
        public const string Extension = ".2";
        public const string Extension2 = ".user";

        public string? GetVersionExt()
        {
            return Option.GameName switch
            {
                _ => Extension
            };
        }

        public override RSZFile? GetRSZ() => RSZ;

        protected override bool DoRead()
        {
            ResourceInfoList.Clear();
            UserdataInfoList.Clear();

            var handler = FileHandler;
            if (!Header.Read(handler)) return false;
            if (Header.Data.magic != Magic)
            {
                throw new InvalidDataException($"{handler.FilePath} Not a SCN file");
            }

            handler.Seek(Header.Data.dataOffset);
            ResourceInfoList.Read(handler, Header.Data.resourceCount);

            handler.Seek(Header.Data.userdataInfoOffset);
            UserdataInfoList.Read(handler, Header.Data.userdataCount);

            RSZ = new RSZFile(Option, FileHandler.WithOffset(Header.Data.dataOffset));
            RSZ.Read(0, false);
            return true;
        }

        protected override bool DoWrite()
        {
            if (StructChanged)
            {
                RebuildInfoTable();
            }
            FileHandler handler = FileHandler;
            handler.Clear();
            ref var header = ref Header.Data;
            handler.Seek(Header.Size);
            handler.Align(16);
            header.resourceInfoOffset = handler.Tell();
            ResourceInfoList.Write(handler);

            handler.Align(16);
            header.userdataInfoOffset = handler.Tell();
            UserdataInfoList.Write(handler);

            handler.StringTableFlush();

            header.dataOffset = handler.Tell();
            // 内部偏移是从0开始算的
            RSZ!.WriteTo(FileHandler.WithOffset(header.dataOffset));

            header.magic = Magic;
            header.resourceCount = ResourceInfoList.Count;
            header.userdataCount = UserdataInfoList.Count;
            Header.Write(handler, 0);
            return true;
        }

        public void RebuildInfoTable()
        {
            RSZ!.RebuildInstanceInfo();
            RszUtils.SyncUserDataFromRsz(UserdataInfoList, RSZ);
            RszUtils.SyncResourceFromRsz(ResourceInfoList, RSZ);
            StructChanged = false;
        }
    }
}
