namespace RszFile
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

        public struct ResourceInfo {
            public ulong pathOffset;
            // public string resourcePath;
        }

        public struct UserdataInfo {
            public uint typeId;
            public uint CRC;
            public ulong pathOffset;
            // public string userdataPath;
        }
    }
}
