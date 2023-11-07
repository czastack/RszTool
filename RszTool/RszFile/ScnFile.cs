namespace RszTool
{
    public class ScnFile
    {

        public struct Header {
            public uint magic;
            public uint infoCount;
            public uint resourceCount;
            public uint folderCount;
            public uint prefabCount;
            public uint userdataCount;
            public ulong folderInfoOffset;
            public ulong resourceInfoOffset;
            public ulong prefabInfoOffset;
            public ulong userdataInfoOffset;
            public ulong dataOffset;
        }

        public struct GameObjectInfo {
            public Guid guid;
            public int objectId;
            public int parentId;
            public short componentCount;
            public short ukn;
            public int prefabId;
        }

        public struct FolderInfo {
            public int objectId;
            public int parentId;
        }

        // ResourceInfo

        public class PrefabInfo : BaseModel {
            public uint pathOffset;
            public string? prefabPath;
            public int parentId;

            public override bool Read(FileHandler handler)
            {
                if (!base.Read(handler)) return false;
                handler.Read(ref pathOffset);
                handler.Read(ref parentId);
                EndRead(handler);
                prefabPath = handler.ReadWString((int)pathOffset);
                return true;
            }

            public override bool Write(FileHandler handler)
            {
                if (!base.Write(handler)) return false;
                handler.Write(ref pathOffset);
                handler.Write(ref parentId);
                return true;
            }
        }

        public StructModel<Header> dataHeader = new();
        public StructModel<GameObjectInfo> dataGameObjectInfo = new();
        public StructModel<FolderInfo> dataFolderInfo = new();
        public ResourceInfo dataRSZUserDataInfo = new();
        public PrefabInfo dataPrefabInfo = new();
        public UserdataInfo dataUserdataInfo = new();

        public ScnFile()
        {
        }

        public bool Read(FileHandler handler)
        {
            if (!dataHeader.Read(handler)) return false;
            if (!dataGameObjectInfo.Read(handler)) return false;
            if (!dataFolderInfo.Read(handler)) return false;
            if (!dataRSZUserDataInfo.Read(handler)) return false;
            if (!dataPrefabInfo.Read(handler)) return false;
            if (!dataUserdataInfo.Read(handler)) return false;
            return true;
        }

        public bool Write(FileHandler handler)
        {
            if (!dataHeader.Write(handler)) return false;
            if (!dataGameObjectInfo.Write(handler)) return false;
            if (!dataFolderInfo.Write(handler)) return false;
            if (!dataRSZUserDataInfo.Write(handler)) return false;
            if (!dataPrefabInfo.Write(handler)) return false;
            if (!dataUserdataInfo.Write(handler)) return false;
            return true;
        }
    }
}
