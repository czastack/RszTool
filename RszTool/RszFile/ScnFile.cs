namespace RszTool
{
    using GameObjectInfoModel = StructModel<ScnFile.GameObjectInfo>;
    using FolderInfoModel = StructModel<ScnFile.FolderInfo>;
    using RszTool.Common;
    using System.Reflection.Metadata.Ecma335;

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
                prefabPath = handler.ReadWString(pathOffset);
                return true;
            }

            protected override bool DoWrite(FileHandler handler)
            {
                handler.StringTableAdd(prefabPath);
                handler.Write(ref pathOffset);
                handler.Write(ref parentId);
                return true;
            }
        }


        public class FolderData
        {
            public WeakReference<FolderData>? ParentRef;
            public FolderInfoModel? Info;
            public List<FolderData> Chidren = new();
            public List<GameObjectData> GameObjects = new();
            public RszInstance? Instance;

            public FolderData? Parent
            {
                get => ParentRef?.GetTarget();
                set => ParentRef = value != null ? new(value) : null;
            }

            public int? ObjectId => Info?.Data.objectId;
        }


        public class GameObjectData
        {
            private WeakReference<FolderData>? FolderRef;
            public WeakReference<GameObjectData>? ParentRef;
            public GameObjectInfoModel? Info;
            public List<RszInstance> Components = new();
            public List<GameObjectData> Chidren = new();
            public RszInstance? Instance;
            public PrefabInfo? Prefab;

            public FolderData? Folder
            {
                get => FolderRef?.GetTarget();
                set => FolderRef = value != null ? new(value) : null;
            }

            public GameObjectData? Parent
            {
                get => ParentRef?.GetTarget();
                set => ParentRef = value != null ? new(value) : null;
            }

            public string? Name => Instance?.GetFieldValue("v0") as string;

            public int? ObjectId => Info?.Data.objectId;
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

        public const uint Magic = 0x4e4353;
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
            if (Header.Data.magic != Magic)
            {
                throw new InvalidDataException($"{handler.FilePath} Not a SCN file");
            }

            GameObjectInfoList.Read(handler, Header.Data.infoCount);

            handler.Seek(Header.Data.folderInfoOffset);
            FolderInfoList.Read(handler, Header.Data.folderCount);

            handler.Seek(Header.Data.resourceInfoOffset);
            ResourceInfoList.Read(handler, Header.Data.resourceCount);

            handler.Seek(Header.Data.prefabInfoOffset);
            PrefabInfoList.Read(handler, Header.Data.prefabCount);

            handler.Seek(Header.Data.userdataInfoOffset);
            UserdataInfoList.Read(handler, Header.Data.userdataCount);

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

            handler.StringTableFlush();

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
        public void SetupGameObjects()
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
                        Instance = RSZ!.GetGameObject(info.Data.objectId),
                    };
                    if (info.Data.parentId == -1)
                    {
                        FolderDatas.Add(folderData);
                    }
                    folderIdxMap[info.Data.objectId] = folderData;
                }
            }

            Dictionary<int, GameObjectData> gameObjectMap = new();
            GameObjectDatas ??= new();
            foreach (var info in GameObjectInfoList)
            {
                GameObjectData gameObjectData = new()
                {
                    Info = info,
                    Instance = RSZ!.GetGameObject(info.Data.objectId),
                };
                for (int i = info.Data.objectId + 1; i < info.Data.objectId + info.Data.componentCount; i++)
                {
                    gameObjectData.Components.Add(RSZ!.GetGameObject(i));
                }
                if (info.Data.prefabId >= 0 && info.Data.prefabId < PrefabInfoList.Count)
                {
                    gameObjectData.Prefab = PrefabInfoList[info.Data.prefabId];
                }
                gameObjectMap[info.Data.objectId] = gameObjectData;
                if (info.Data.parentId == -1)
                {
                    GameObjectDatas.Add(gameObjectData);
                }
            }

            // add children and set parent
            foreach (var info in GameObjectInfoList)
            {
                var gameObject = gameObjectMap[info.Data.objectId];
                if (gameObjectMap.TryGetValue(info.Data.parentId, out var parent))
                {
                    parent.Chidren.Add(gameObject);
                    gameObject.Parent = parent;
                }
                if (folderIdxMap.TryGetValue(info.Data.parentId, out var folder))
                {
                    folder.GameObjects.Add(gameObject);
                    gameObject.Folder = folder;
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

        /// <summary>
        /// 收集GameObject以及子物体的实例和组件实例
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="rszInstances"></param>
        public static void CollectGameObjectInstances(GameObjectData gameObject, List<RszInstance> rszInstances)
        {
            rszInstances.Add(gameObject.Instance!);
            foreach (var item in gameObject.Components)
            {
                rszInstances.Add(item);
            }
            foreach (var child in gameObject.Chidren)
            {
                CollectGameObjectInstances(child, rszInstances);
            }
        }

        /// <summary>
        /// 收集GameObject以及子物体的实例和组件实例
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="rszInstances"></param>
        public static void CollectFolderGameObjectInstances(FolderData folder, List<RszInstance> rszInstances)
        {
            rszInstances.Add(folder.Instance!);
            foreach (var gameObject in folder.GameObjects)
            {
                CollectGameObjectInstances(gameObject, rszInstances);
            }
            foreach (var child in folder.Chidren)
            {
                CollectFolderGameObjectInstances(child, rszInstances);
            }
        }

        /// <summary>
        /// 根据GameObjectDatas和FolderDatas重建其他表
        /// </summary>
        public void RebuildInfoTable()
        {
            if (RSZ == null)
            {
                throw new InvalidOperationException("RSZ is null");
            }

            // 重新生成实例表
            List<RszInstance> rszInstances = new();
            if (GameObjectDatas != null)
            {
                foreach (var gameObjectData in GameObjectDatas)
                {
                    CollectGameObjectInstances(gameObjectData, rszInstances);
                }
            }

            if (FolderDatas != null)
            {
                foreach (var folder in FolderDatas)
                {
                    CollectFolderGameObjectInstances(folder, rszInstances);
                }
            }

            RSZ.InstanceListUnflatten(rszInstances);
            RSZ.InstanceList.Clear();
            RSZ.InstanceListFlatten(rszInstances);
            RSZ.RebulidInstanceInfo(false, false);
            foreach (var instance in rszInstances)
            {
                instance.ObjectTableIndex = -1;
            }
            RSZ.ObjectTableList.Clear();

            // 重新构建
            GameObjectInfoList.Clear();
            FolderInfoList.Clear();
            PrefabInfoList.Clear();
            if (GameObjectDatas != null)
            {
                foreach (var gameObjectData in GameObjectDatas)
                {
                    RebuildGameObjectInfoRecursion(gameObjectData);
                }
            }

            if (FolderDatas != null)
            {
                foreach (var folder in FolderDatas)
                {
                    RebuildFolderInfoRecursion(folder);
                }
            }
        }

        private void RebuildGameObjectInfoRecursion(GameObjectData gameObject)
        {
            var instance = gameObject.Instance!;
            if (instance.ObjectTableIndex != -1) return;
            int prefabId = gameObject.Prefab == null ? -1 :
                PrefabInfoList.GetIndexOrAdd(gameObject.Prefab);;
            ref var infoData = ref gameObject.Info!.Data;
            RSZ!.AddToObjectTable(instance);
            infoData.objectId = instance.ObjectTableIndex;
            infoData.componentCount = (short)gameObject.Components.Count;
            infoData.parentId = gameObject.Parent?.ObjectId ?? gameObject.Folder?.ObjectId ?? -1;
            infoData.prefabId = prefabId;

            GameObjectInfoList.Add(gameObject.Info!);
            foreach (var item in gameObject.Components)
            {
                RSZ!.AddToObjectTable(item);
            }
            foreach (var child in gameObject.Chidren)
            {
                RebuildGameObjectInfoRecursion(child);
            }
        }

        private void RebuildFolderInfoRecursion(FolderData folder)
        {
            RSZ!.AddToObjectTable(folder.Instance!);
            ref var infoData = ref folder.Info!.Data;
            infoData.objectId = folder.Instance!.ObjectTableIndex;
            infoData.parentId = folder.Parent?.ObjectId ?? -1;
            FolderInfoList.Add(folder.Info!);
            foreach (var gameObject in folder.GameObjects)
            {
                RebuildGameObjectInfoRecursion(gameObject);
            }
            foreach (var child in folder.Chidren)
            {
                RebuildFolderInfoRecursion(child);
            }
        }

        /// <summary>
        /// 提取某个游戏对象，构造新的RSZ
        /// 暂时只支持没有Parent的GameObject
        /// </summary>
        /// <param name="name"></param>
        public bool ExtractGameObjectRSZ(string name, RSZFile newRSZ)
        {
            if (GameObjectDatas == null)
            {
                Console.Error.WriteLine("GameObjectDatas is null");
                return false;
            }
            GameObjectData? gameObject = null;
            foreach (var item in GameObjectDatas)
            {
                if (item.Name == name)
                {
                    gameObject = item;
                    break;
                }
            }
            if (gameObject == null)
            {
                Console.Error.WriteLine($"GameObject {name} not found");
                return false;
            }
            if (RSZ == null)
            {
                Console.Error.WriteLine($"RSZ is null");
                return false;
            }

            List<RszInstance> rszInstances = new();

            // self InstanceUnflatten
            CollectGameObjectInstances(gameObject, rszInstances);
            RSZ.InstanceListUnflatten(rszInstances);

            // new InstanceFlatten
            newRSZ.InstanceListFlatten(rszInstances);

            newRSZ.RebulidInstanceInfo(false);
            // foreach (var item in newRSZ.InstanceList)
            // {
            //     Console.WriteLine(newRSZ.InstanceStringify(item));
            // }
            // write
            newRSZ.Write();

            // new InstanceUnflatten
            newRSZ.InstanceListUnflatten(rszInstances);

            // self InstanceFlatten
            RSZ.InstanceListFlatten(rszInstances);
            return true;
        }
    }
}
