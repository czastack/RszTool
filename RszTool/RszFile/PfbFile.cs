namespace RszTool
{
    using GameObjectInfoModel = StructModel<PfbFile.GameObjectInfo>;
    using GameObjectRefInfoModel = StructModel<PfbFile.GameObjectRefInfo>;

    public class PfbFile : BaseRszFile
    {
        public struct HeaderStruct {
            public uint magic;
            public int infoCount;
            public int resourceCount;
            public int gameObjectRefInfoCount;
            public long userdataCount;
            public long gameObjectRefInfoOffset;
            public long resourceInfoOffset;
            public long userdataInfoOffset;
            public long dataOffset;
        }

        public struct GameObjectInfo {
            public int objectId;
            public int parentId;
            public int componentCount;
        }

        public struct GameObjectRefInfo {
            public uint objectId;
            public int propertyId;
            public int arrayIndex;
            public uint targetId;
        }

        // ResourceInfo
        // UserdataInfo

        public StructModel<HeaderStruct> Header = new();
        public List<GameObjectInfoModel> GameObjectInfoList = new();
        public List<GameObjectRefInfoModel> GameObjectRefInfoList = new();
        public List<ResourceInfo> ResourceInfoList = new();
        public List<UserdataInfo> UserdataInfoList = new();
        public RSZFile? RSZ { get; private set; }

        public PfbFile(RszFileOption option, FileHandler fileHandler) : base(option, fileHandler)
        {
        }

        public const uint Magic = 0x424650;
        public const string Extension2 = ".pfb";

        public string? GetExtension()
        {
            return Option.GameName switch
            {
                "re2" => Option.TdbVersion == 66 ? ".16" : ".17",
                "re3" => ".17",
                "re4" => ".17",
                "re8" => ".17",
                "re7" => Option.TdbVersion == 49 ? ".16" : ".17",
                "dmc5" =>".16",
                "mhrise" => ".17",
                "sf6" => ".17",
                _ => null
            };
        }

        protected override bool DoRead()
        {
            FileHandler handler = FileHandler;

            if (!Header.Read(handler)) return false;
            if (Header.Data.magic != Magic)
            {
                throw new InvalidDataException($"{handler.FilePath} Not a PFB file");
            }

            GameObjectInfoList.Read(handler, Header.Data.infoCount);

            handler.Seek(Header.Data.gameObjectRefInfoOffset);
            GameObjectRefInfoList.Read(handler, Header.Data.gameObjectRefInfoCount);

            handler.Seek(Header.Data.resourceInfoOffset);
            ResourceInfoList.Read(handler, Header.Data.resourceCount);

            handler.Seek(Header.Data.userdataInfoOffset);
            UserdataInfoList.Read(handler, (int)Header.Data.userdataCount);

            RSZ = new RSZFile(Option, FileHandler.WithOffset(Header.Data.dataOffset));
            RSZ.Read(0, false);
            if (RSZ.ObjectTableList.Count > 0)
            {
                // SetupGameObjects();
            }
            return true;
        }

        protected override bool DoWrite()
        {
            FileHandler handler = FileHandler;

            handler.Seek(Header.Size);
            handler.Align(16);
            GameObjectInfoList.Write(handler);

            if (Header.Data.gameObjectRefInfoCount > 0)
            {
                // handler.Align(16);
                Header.Data.gameObjectRefInfoOffset = handler.Tell();
                GameObjectRefInfoList.Write(handler);
            }

            handler.Align(16);
            Header.Data.resourceInfoOffset = handler.Tell();
            ResourceInfoList.Write(handler);

            if (UserdataInfoList.Count > 0)
            {
                handler.Align(16);
                Header.Data.userdataInfoOffset = handler.Tell();
                UserdataInfoList.Write(handler);
            }

            handler.StringTableFlush();

            handler.Align(16);
            Header.Data.dataOffset = handler.Tell();
            RSZ!.WriteTo(FileHandler.WithOffset(Header.Data.dataOffset));

            Header.Data.infoCount = GameObjectInfoList.Count;
            Header.Data.resourceCount = ResourceInfoList.Count;
            Header.Data.gameObjectRefInfoCount = GameObjectRefInfoList.Count;
            Header.Data.userdataCount = UserdataInfoList.Count;
            Header.Rewrite(handler);

            return true;
        }
    }
}
