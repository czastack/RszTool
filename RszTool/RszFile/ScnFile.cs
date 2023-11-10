namespace RszTool
{
    using GameObjectInfoModel = StructModel<ScnFile.GameObjectInfo>;
    using FolderInfoModel = StructModel<ScnFile.FolderInfo>;

    public class ScnFile : BaseRszFile
    {
        public struct HeaderStruct {
            public uint magic;
            public int infoCount;
            public int resourceCount;
            public int folderCount;
            public int prefabCount;
            public int userdataCount;
            public long folderInfoOffset;
            public long resourceInfoOffset;
            public long prefabInfoOffset;
            public long userdataInfoOffset;
            public long dataOffset;
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

            protected override bool DoRead(FileHandler handler)
            {
                handler.Read(ref pathOffset);
                handler.Read(ref parentId);
                prefabPath = handler.ReadWString((long)pathOffset);
                return true;
            }

            protected override bool DoWrite(FileHandler handler)
            {
                handler.AddStringToWrite(prefabPath);
                handler.Write(ref pathOffset);
                handler.Write(ref parentId);
                return true;
            }
        }

        public class FolderData
        {
            public FolderInfoModel? Info;
            public List<FolderData> Chidren = new();
            public List<GameObjectData> GameObjects = new();
            public RszInstance? RszInstance;
        }


        public class GameObjectData
        {
            public GameObjectInfoModel? Info;
            public List<RszInstance> Components = new();
            public List<GameObjectData> Chidren = new();
            public RszInstance? GameObject;
            public PrefabInfo? Prefab;
        }

        public StructModel<HeaderStruct> Header { get; } = new();
        public List<GameObjectInfoModel> GameObjectInfoList = new();
        // public Dictionary<int, GameObjectInfoModel> GameObjectInfoIdMap = new();
        public List<FolderInfoModel> FolderInfoList = new();
        public List<ResourceInfo> ResourceInfoList = new();
        public List<PrefabInfo> PrefabInfoList = new();
        public List<UserdataInfo> UserdataInfoList = new();
        public RSZFile? RSZ { get; private set; }

        public List<FolderData>? FolderDatas { get; set; }
        public List<GameObjectData>? GameObjectDatas { get; set; }

        public ScnFile(RszFileOption option, FileHandler fileHandler) : base(option, fileHandler)
        {
        }

        public const uint Magic = 5129043;
        public const string Extension2 = ".scn";

        public string? GetExtension()
        {
            return Option.GameName switch
            {
                "re2" => Option.TdbVersion == 66 ? ".19" : ".20",
                "re3" => ".20",
                "re4" => ".20",
                "re8" => ".20",
                "re7" => Option.TdbVersion == 49 ? ".18" : ".20",
                "dmc5" =>".19",
                "mhrise" => ".20",
                "sf6" => ".20",
                _ => null
            };
        }

        protected override bool DoRead()
        {
            FileHandler handler = FileHandler;
            if (!Header.Read(handler)) return false;

            for (int i = 0; i < Header.Data.infoCount; i++)
            {
                GameObjectInfoModel gameObjectInfo = new();
                gameObjectInfo.Read(handler);
                GameObjectInfoList.Add(gameObjectInfo);
                // GameObjectInfoIdMap[gameObjectInfo.Data.objectId] = gameObjectInfo;
            }

            handler.Seek(Header.Data.folderInfoOffset);
            for (int i = 0; i < Header.Data.folderCount; i++)
            {
                FolderInfoModel folderInfo = new();
                folderInfo.Read(handler);
                FolderInfoList.Add(folderInfo);
            }

            handler.Seek(Header.Data.resourceInfoOffset);
            for (int i = 0; i < Header.Data.resourceCount; i++)
            {
                ResourceInfo resourceInfo = new();
                resourceInfo.Read(handler);
                ResourceInfoList.Add(resourceInfo);
            }

            handler.Seek(Header.Data.prefabInfoOffset);
            for (int i = 0; i < Header.Data.prefabCount; i++)
            {
                PrefabInfo prefabInfo = new();
                prefabInfo.Read(handler);
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

            if (FolderInfoList.Count > 0)
            {
                handler.Align(16);
                Header.Data.folderInfoOffset = handler.Tell();
                FolderInfoList.Write(handler);
            }

            handler.Align(16);
            Header.Data.resourceInfoOffset = handler.Tell();
            ResourceInfoList.Write(handler);

            if (PrefabInfoList.Count > 0)
            {
                handler.Align(16);
                Header.Data.prefabInfoOffset = handler.Tell();
                PrefabInfoList.Write(handler);
            }

            handler.Align(16);
            Header.Data.userdataInfoOffset = handler.Tell();
            UserdataInfoList.Write(handler);

            handler.FlushStringToWrite();

            handler.Align(16);
            Header.Data.dataOffset = handler.Tell();
            RSZ!.WriteTo(FileHandler.WithOffset(Header.Data.dataOffset));

            Header.Data.infoCount = GameObjectInfoList.Count;
            Header.Data.folderCount = FolderInfoList.Count;
            Header.Data.resourceCount = ResourceInfoList.Count;
            Header.Data.prefabCount = PrefabInfoList.Count;
            Header.Data.userdataCount = UserdataInfoList.Count;
            Header.Rewrite(handler);

            return true;
        }

        public void SaveAsPfb(string path)
        {

        }

        /// <summary>
        /// 解析关联的关系，形成树状结构
        /// </summary>
        private void SetupGameObjects()
        {
            Dictionary<int, FolderData> folderIdxMap = new();
            if (FolderInfoList.Count > 0)
            {
                FolderDatas ??= new();
                foreach (var info in FolderInfoList)
                {
                    FolderData folderData = new()
                    {
                        Info = info,
                        RszInstance = RSZ!.GetGameObject(info.Data.objectId),
                    };
                    FolderDatas.Add(folderData);
                    folderIdxMap[info.Data.objectId] = folderData;
                }
            }

            Dictionary<int, GameObjectData> gameObjParentMap = new();
            GameObjectDatas ??= new();
            foreach (var info in GameObjectInfoList)
            {
                GameObjectData gameObjectData = new()
                {
                    Info = info,
                    GameObject = RSZ!.GetGameObject(info.Data.objectId),
                };
                for (int i = info.Data.objectId + 1; i < info.Data.objectId + info.Data.componentCount; i++)
                {
                    gameObjectData.Components.Add(RSZ!.GetGameObject(i));
                }
                if (info.Data.prefabId >= 0 && info.Data.prefabId < PrefabInfoList.Count)
                {
                    gameObjectData.Prefab = PrefabInfoList[info.Data.prefabId];
                }
                gameObjParentMap[info.Data.objectId] = gameObjectData;
                GameObjectDatas.Add(gameObjectData);
            }

            foreach (var info in GameObjectInfoList)
            {
                if (gameObjParentMap.TryGetValue(info.Data.parentId, out var parent))
                {
                    parent.Chidren.Add(gameObjParentMap[info.Data.objectId]);
                }
                if (folderIdxMap.TryGetValue(info.Data.parentId, out var folder))
                {
                    folder.GameObjects.Add(gameObjParentMap[info.Data.objectId]);
                }
            }

            foreach (var info in FolderInfoList)
            {
                if (folderIdxMap.TryGetValue(info.Data.parentId, out var folder))
                {
                    folder.Chidren.Add(folderIdxMap[info.Data.objectId]);
                }
            }
        }
    }
}
