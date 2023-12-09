using System.Collections.ObjectModel;
using RszTool.Common;

namespace RszTool
{
    using GameObjectInfoModel = StructModel<PfbFile.GameObjectInfo>;
    using GameObjectRefInfoModel = StructModel<PfbFile.GameObjectRefInfo>;

    public class PfbFile : BaseRszFile
    {
        public class HeaderStruct : BaseModel {
            public uint magic;
            public int infoCount;
            public int resourceCount;
            public int gameObjectRefInfoCount;
            public long userdataCount;
            public long gameObjectRefInfoOffset;
            public long resourceInfoOffset;
            public long userdataInfoOffset;
            public long dataOffset;

            private GameVersion Version { get; }

            public HeaderStruct(GameVersion version) {
                Version = version;
            }

            protected override bool DoRead(FileHandler handler)
            {
                handler.Read(ref magic);
                handler.Read(ref infoCount);
                handler.Read(ref resourceCount);
                handler.Read(ref gameObjectRefInfoCount);
                if (Version > GameVersion.re2) handler.Read(ref userdataCount);
                handler.Read(ref gameObjectRefInfoOffset);
                handler.Read(ref resourceInfoOffset);
                if (Version > GameVersion.re2) handler.Read(ref userdataInfoOffset);
                handler.Read(ref dataOffset);
                return true;
            }

            protected override bool DoWrite(FileHandler handler)
            {
                handler.Write(ref magic);
                handler.Write(ref infoCount);
                handler.Write(ref resourceCount);
                handler.Write(ref gameObjectRefInfoCount);
                if (Version > GameVersion.re2) handler.Write(ref userdataCount);
                handler.Write(ref gameObjectRefInfoOffset);
                handler.Write(ref resourceInfoOffset);
                if (Version > GameVersion.re2) handler.Write(ref userdataInfoOffset);
                handler.Write(ref dataOffset);
                return true;
            }
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

        public class GameObjectData : IGameObjectData
        {
            private WeakReference<GameObjectData>? parentRef;
            public GameObjectInfoModel? Info { get; set; }
            public ObservableCollection<RszInstance> Components { get; private set; } = new();
            public ObservableCollection<GameObjectData> Children { get; private set; } = new();
            public RszInstance? Instance { get; set; }

            /// <summary>
            /// 从ScnFile.GameObjectData生成GameObjectData
            /// </summary>
            /// <param name="scnGameObject"></param>
            /// <returns></returns>
            public static GameObjectData FromScnGameObject(ScnFile.GameObjectData scnGameObject)
            {
                RszInstance.CleanCloneCache();
                GameObjectData gameObject = new()
                {
                    Info = new()
                    {
                        Data = new GameObjectInfo
                        {
                            // objectId 和 parentId 应该重新生成
                            componentCount = scnGameObject.Components.Count,
                        }
                    },
                    Components = new(scnGameObject.Components.Select(item => item.CloneCached())),
                    Instance = scnGameObject.Instance?.CloneCached()
                };
                foreach (var child in scnGameObject.Children)
                {
                    var newChild = FromScnGameObject(child);
                    newChild.Parent = gameObject;
                    gameObject.Children.Add(newChild);
                }
                RszInstance.CleanCloneCache();
                return gameObject;
            }

            public GameObjectData? Parent
            {
                get => parentRef?.GetTarget();
                set => parentRef = value != null ? new(value) : null;
            }

            public string? Name => (Instance?.GetFieldValue("v0") ?? Instance?.GetFieldValue("Name")) as string;

            public int? ObjectId => Info?.Data.objectId;

            public override string ToString()
            {
                return Name ?? "";
            }
        }

        // ResourceInfo
        // UserdataInfo

        public HeaderStruct Header { get; }
        public List<GameObjectInfoModel> GameObjectInfoList { get; } = new();
        public List<GameObjectRefInfoModel> GameObjectRefInfoList { get; } = new();
        public List<ResourceInfo> ResourceInfoList { get; } = new();
        public List<UserdataInfo> UserdataInfoList { get; } = new();
        public RSZFile? RSZ { get; private set; }
        public ObservableCollection<GameObjectData>? GameObjectDatas { get; set; }

        public PfbFile(RszFileOption option, FileHandler fileHandler) : base(option, fileHandler)
        {
            Header = new(option.Version);
            if (fileHandler.FilePath != null)
            {
                RszUtils.CheckFileExtension(fileHandler.FilePath, Extension2, GetVersionExt());
            }
        }

        public const uint Magic = 0x424650;
        public const string Extension2 = ".pfb";

        public string? GetVersionExt()
        {
            return Option.GameName switch
            {
                GameName.re2 => ".16",
                GameName.re2rt => ".17",
                GameName.re3 => ".17",
                GameName.re4 => ".17",
                GameName.re8 => ".17",
                GameName.re7 => ".16",
                GameName.re7rt => ".17",
                GameName.dmc5 =>".16",
                GameName.mhrise => ".17",
                GameName.sf6 => ".17",
                _ => null
            };
        }

        public override RSZFile? GetRSZ() => RSZ;

        protected override bool DoRead()
        {
            GameObjectInfoList.Clear();
            GameObjectRefInfoList.Clear();
            ResourceInfoList.Clear();
            UserdataInfoList.Clear();

            var handler = FileHandler;
            var header = Header;
            if (!header.Read(handler)) return false;
            if (header.magic != Magic)
            {
                throw new InvalidDataException($"{handler.FilePath} Not a PFB file");
            }

            GameObjectInfoList.Read(handler, header.infoCount);

            handler.Seek(header.gameObjectRefInfoOffset);
            GameObjectRefInfoList.Read(handler, header.gameObjectRefInfoCount);

            handler.Seek(header.resourceInfoOffset);
            // ResourceInfoList.Read(handler, header.resourceCount);
            for (int i = 0; i < header.resourceCount; i++)
            {
                ResourceInfo item = new();
                if (Option.Version <= GameVersion.re2) item.HasOffset = false;
                if (!item.Read(handler)) return false;
                ResourceInfoList.Add(item);
            }

            handler.Seek(header.userdataInfoOffset);
            if (Option.Version > GameVersion.re2)
            {
                UserdataInfoList.Read(handler, (int)header.userdataCount);
            }

            RSZ = new RSZFile(Option, FileHandler.WithOffset(header.dataOffset));
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
            var header = Header;
            handler.Seek(header.Size);
            GameObjectInfoList.Write(handler);

            if (header.gameObjectRefInfoCount > 0)
            {
                // handler.Align(16);
                header.gameObjectRefInfoOffset = handler.Tell();
                GameObjectRefInfoList.Write(handler);
            }

            handler.Align(16);
            header.resourceInfoOffset = handler.Tell();
            // ResourceInfoList.Write(handler);
            foreach (var item in ResourceInfoList)
            {
                if (Option.Version <= GameVersion.re2) item.HasOffset = false;
                if (!item.Write(handler)) return false;
            }

            if (Option.Version > GameVersion.re2 && UserdataInfoList.Count > 0)
            {
                handler.Align(16);
                header.userdataInfoOffset = handler.Tell();
                UserdataInfoList.Write(handler);
            }

            handler.StringTableFlush();

            handler.Align(16);
            header.dataOffset = handler.Tell();
            RSZ!.WriteTo(FileHandler.WithOffset(header.dataOffset));

            header.magic = Magic;
            header.infoCount = GameObjectInfoList.Count;
            header.resourceCount = ResourceInfoList.Count;
            header.gameObjectRefInfoCount = GameObjectRefInfoList.Count;
            header.userdataCount = UserdataInfoList.Count;
            header.Write(handler, 0);

            return true;
        }

        /// <summary>
        /// 解析关联的关系，形成树状结构
        /// </summary>
        public void SetupGameObjects()
        {
            Dictionary<int, GameObjectData> gameObjectMap = new();
            GameObjectDatas ??= new();
            foreach (var info in GameObjectInfoList)
            {
                GameObjectData gameObjectData = new()
                {
                    Info = info,
                    Instance = RSZ!.ObjectList[info.Data.objectId],
                };
                for (int i = 0; i < info.Data.componentCount; i++)
                {
                    gameObjectData.Components.Add(RSZ!.ObjectList[info.Data.objectId + 1 + i]);
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
                    parent.Children.Add(gameObject);
                    gameObject.Parent = parent;
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
            foreach (var child in gameObject.Children)
            {
                CollectGameObjectInstances(child, rszInstances);
            }
        }

        /// <summary>
        /// 根据GameObjectDatas和FolderDatas重建其他表
        /// </summary>
        public void RebuildInfoTable()
        {
            RSZ ??= new(Option, FileHandler);

            // 重新生成实例表
            List<RszInstance> rszInstances = new() { RszInstance.NULL };
            if (GameObjectDatas != null)
            {
                foreach (var gameObjectData in GameObjectDatas)
                {
                    CollectGameObjectInstances(gameObjectData, rszInstances);
                }
            }

            RSZ.ClearInstances();
            RSZ.FixInstanceListIndex(rszInstances);
            RSZ.RebuildInstanceInfo(false, false);
            foreach (var instance in rszInstances)
            {
                instance.ObjectTableIndex = -1;
            }
            RSZ.ClearObjects();

            // 重新构建
            GameObjectInfoList.Clear();
            if (GameObjectDatas != null)
            {
                foreach (var gameObjectData in GameObjectDatas)
                {
                    AddGameObjectInfoRecursion(gameObjectData);
                }
            }

            RszUtils.SyncUserDataFromRsz(UserdataInfoList, RSZ);
            RszUtils.SyncResourceFromRsz(ResourceInfoList, RSZ);
            StructChanged = false;
        }

        private void AddGameObjectInfoRecursion(GameObjectData gameObject)
        {
            var instance = gameObject.Instance!;
            if (instance.ObjectTableIndex != -1) return;
            ref var infoData = ref gameObject.Info!.Data;
            RSZ!.AddToObjectTable(instance);
            infoData.objectId = instance.ObjectTableIndex;
            infoData.parentId = gameObject.Parent?.ObjectId ?? -1;
            infoData.componentCount = (short)gameObject.Components.Count;

            GameObjectInfoList.Add(gameObject.Info);
            foreach (var item in gameObject.Components)
            {
                RSZ!.AddToObjectTable(item);
            }
            foreach (var child in gameObject.Children)
            {
                AddGameObjectInfoRecursion(child);
            }
        }

        /// <summary>
        /// 从ScnFile.GameObjectData生成Pfb
        /// </summary>
        /// <param name="scnGameObject"></param>
        public void PfbFromScnGameObject(ScnFile.GameObjectData scnGameObject)
        {
            GameObjectData gameObject = GameObjectData.FromScnGameObject(scnGameObject);
            if (GameObjectDatas == null)
            {
                GameObjectDatas = new();
            }
            else
            {
                GameObjectDatas.Clear();
            }
            GameObjectDatas.Add(gameObject);
            RebuildInfoTable();
        }
    }
}
