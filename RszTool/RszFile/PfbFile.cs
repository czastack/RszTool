namespace RszTool
{
    public class PfbFile
    {
        public struct Header {
            public uint magic;
            public uint infoCount;
            public uint resourceCount;
            public uint gameObjectRefInfoCount;
            public ulong userdataCount;
            public ulong gameObjectRefInfoOffset;
            public ulong resourceInfoOffset;
            public ulong userdataInfoOffset;
            public ulong dataOffset;
        }

        public struct GameObjectInfo {
            public int objectId;
            public int parentId;
            public int componentCount;
        }

        public struct GameObjectRefInfo {
            public uint objectID;
            public int propertyId;
            public int arrayIndex;
            public uint targetId;
        }

        // ResourceInfo

        public struct UserdataInfo {
            public uint typeId;
            public uint CRC;
            public ulong pathOffset;
            // public string userdataPath;
        }

        public StructModel<Header> dataHeader = new();
        public StructModel<GameObjectInfo> dataGameObjectInfo = new();
        public StructModel<GameObjectRefInfo> dataGameObjectRefInfo = new();
        public ResourceInfo dataRSZUserDataInfo = new();
        public StructModel<UserdataInfo> dataUserdataInfo = new();

        public PfbFile()
        {
        }

        public bool Read(FileHandler handler)
        {
            if (!dataHeader.Read(handler)) return false;
            if (!dataGameObjectInfo.Read(handler)) return false;
            if (!dataGameObjectRefInfo.Read(handler)) return false;
            if (!dataRSZUserDataInfo.Read(handler)) return false;
            if (!dataUserdataInfo.Read(handler)) return false;
            return true;
        }

        public bool Write(FileHandler handler)
        {
            if (!dataHeader.Write(handler)) return false;
            if (!dataGameObjectInfo.Write(handler)) return false;
            if (!dataGameObjectRefInfo.Write(handler)) return false;
            if (!dataRSZUserDataInfo.Write(handler)) return false;
            if (!dataUserdataInfo.Write(handler)) return false;
            return true;
        }
    }
}
