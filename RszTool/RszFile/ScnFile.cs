namespace RszFile
{
    public class ScnFile
    {

        class Header {
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

        class GameObjectInfo {
            public Guid guid;
            public int objectId;
            public int parentId;
            public short componentCount;
            public short ukn;
            public int prefabId;
        }

        class FolderInfo {
            public int objectId;
            public int parentId;
        }

        class ResourceInfo {
            public ulong pathOffset;
            // public string resourcePath;
        }

        class PrefabInfo {
            public uint pathOffset;
            // public string prefabPath;
            public int parentId;
        }

        class UserdataInfo {
            public uint typeId;
            public uint CRC;
            public ulong pathOffset;
            // public string userdataPath;
        }
    }
}
