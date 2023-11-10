namespace RszTool
{
    public class RSZFile : BaseRszFile
    {
        public struct HeaderStruct
        {
            public uint magic;
            public uint version;
            public int objectCount;
            public int instanceCount;
            public long userdataCount;
            public long instanceOffset;
            public long dataOffset;
            public long userdataOffset;
        }

        public struct ObjectTable
        {
            public int instanceId;
        }

        public StructModel<HeaderStruct> Header { get; } = new();
        public List<StructModel<ObjectTable>> ObjectTableList { get; } = new();
        public List<InstanceInfo> InstanceInfoList { get; } = new();
        public List<IRSZUserDataInfo> RSZUserDataInfoList { get; } = new();

        public List<RszInstance> InstanceList { get; } = new();
        public List<RSZFile>? EmbeddedRSZFileList { get; private set; }

        public RSZFile(RszFileOption option, FileHandler fileHandler) : base(option, fileHandler)
        {
        }

        protected override bool DoRead()
        {
            var handler = FileHandler;
            if (!Header.Read(handler)) return false;
            var rszParser = RszParser;

            for (int i = 0; i < Header.Data.objectCount; i++)
            {
                StructModel<ObjectTable> objectTable = new();
                objectTable.Read(handler);
                ObjectTableList.Add(objectTable);
            }

            handler.Seek(Header.Data.instanceOffset);
            for (int i = 0; i < Header.Data.instanceCount; i++)
            {
                InstanceInfo instanceInfo = new();
                instanceInfo.Read(handler);
                instanceInfo.ReadClassName(rszParser);
                InstanceInfoList.Add(instanceInfo);
            }

            handler.Seek(Header.Data.userdataOffset);
            Dictionary<uint, int> distanceIdToIndex = new();
            if (Option.TdbVersion < 67)
            {
                if (Header.Data.userdataCount > 0)
                {
                    EmbeddedRSZFileList = new();
                    for (int i = 0; i < (int)Header.Data.userdataCount; i++)
                    {
                        RSZUserDataInfo_TDB_LE_67 rszUserDataInfo = new();
                        rszUserDataInfo.Read(handler);
                        rszUserDataInfo.ReadClassName(rszParser);
                        RSZUserDataInfoList.Add(rszUserDataInfo);
                        distanceIdToIndex[rszUserDataInfo.instanceId] = i;

                        RSZFile embeddedRSZFile = new(Option, handler.WithOffset(rszUserDataInfo.RSZOffset));
                        embeddedRSZFile.Read();
                        EmbeddedRSZFileList.Add(embeddedRSZFile);
                    }
                }
            }
            else
            {
                for (int i = 0; i < (int)Header.Data.userdataCount; i++)
                {
                    RSZUserDataInfo rszUserDataInfo = new();
                    rszUserDataInfo.Read(handler);
                    RSZUserDataInfoList.Add(rszUserDataInfo);
                    distanceIdToIndex[rszUserDataInfo.instanceId] = i;
                }
            }

            // read instance data
            for (int i = 0; i < InstanceInfoList.Count; i++)
            {
                RszClass? rszClass = RszParser.GetRSZClass(InstanceInfoList[i].typeId);
                if (rszClass == null)
                {
                    // throw new InvalidDataException($"RszClass {InstanceInfoList[i].typeId} not found!");
                    Console.Error.WriteLine($"RszClass {InstanceInfoList[i].typeId} not found!");
                    continue;
                }
                if (!distanceIdToIndex.TryGetValue((uint)i, out int userDataIdx))
                {
                    userDataIdx = -1;
                }
                RszInstance instance = new(rszClass, i, userDataIdx);
                instance.Read(handler);
                if (userDataIdx != -1)
                {
                    instance.RSZUserData = RSZUserDataInfoList[userDataIdx];
                }
                InstanceList.Add(instance);
            }
            return true;
        }

        protected override bool DoWrite()
        {
            var handler = FileHandler;

            handler.Seek(Header.Size);

            handler.Align(16);
            ObjectTableList.Write(handler);

            handler.Align(16);
            Header.Data.instanceOffset = handler.Tell();
            InstanceInfoList.Write(handler);

            handler.Align(16);
            Header.Data.userdataOffset = handler.Tell();
            RSZUserDataInfoList.Write(handler);

            handler.Align(16);
            handler.FlushStringToWrite();

            // embedded userdata
            if (Option.TdbVersion <= 67 && EmbeddedRSZFileList != null)
            {
                for (int i = 0; i < RSZUserDataInfoList.Count; i++)
                {
                    handler.Align(16);
                    var item = (RSZUserDataInfo_TDB_LE_67)RSZUserDataInfoList[i];
                    item.RSZOffset = handler.Tell();
                    var embeddedRSZ = EmbeddedRSZFileList[i];
                    embeddedRSZ.Write();
                    // rewrite
                    item.dataSize = (uint)embeddedRSZ.Size;
                    item.Rewrite(handler);
                }
            }

            handler.Align(16);
            Header.Data.dataOffset = handler.Tell();
            InstanceList.Write(handler);

            Header.Data.objectCount = ObjectTableList.Count;
            Header.Data.instanceCount = InstanceList.Count;
            Header.Data.userdataCount = RSZUserDataInfoList.Count;
            Header.Rewrite(handler);
            return true;
        }

        public RszInstance GetGameObject(int objectIndex)
        {
            return InstanceList[ObjectTableList[objectIndex].Data.instanceId];
        }

        public RszInstance? CreateInstance(string className, int index = -1)
        {
            RszClass? rszClass = RszParser.GetRSZClass(className);
            if (index == -1)
            {
                index = InstanceList.Count;
            }
            if (rszClass == null) return null;
            RszInstance instance = new(rszClass, index, -1)
            {
                Start = Start + Size
            };
            // TODO Values
            return instance;
        }
    }


    public class InstanceInfo : BaseModel
    {
        public uint typeId;
        public uint CRC;
        public string? ClassName { get; set; }

        protected override bool DoRead(FileHandler handler)
        {
            handler.Read(ref typeId);
            handler.Read(ref CRC);
            return true;
        }

        protected override bool DoWrite(FileHandler handler)
        {
            handler.Write(typeId);
            handler.Write(CRC);
            return true;
        }

        public void ReadClassName(RszParser parser)
        {
            ClassName = parser.GetRSZClassName(typeId);
        }
    }

    public interface IRSZUserDataInfo : IModel
    {
        uint InstanceId { get; }
        uint TypeId { get; }
        string? ClassName { get; }

    }

    public class RSZUserDataInfo : BaseModel, IRSZUserDataInfo
    {
        public uint instanceId;
        public uint typeId;
        public ulong pathOffset;
        public string? path;

        public uint InstanceId => instanceId;
        public uint TypeId => typeId;
        public string? ClassName { get; set; }

        protected override bool DoRead(FileHandler handler)
        {
            handler.Read(ref instanceId);
            handler.Read(ref typeId);
            handler.Read(ref pathOffset);
            path = handler.ReadWString((long)pathOffset);
            return true;
        }

        protected override bool DoWrite(FileHandler handler)
        {
            handler.Write(instanceId);
            handler.Write(typeId);
            handler.AddStringToWrite(path);
            handler.Write(pathOffset);
            return true;
        }

        public void ReadClassName(RszParser parser)
        {
            ClassName = parser.GetRSZClassName(typeId);
        }
    }

    public class RSZUserDataInfo_TDB_LE_67 : BaseModel, IRSZUserDataInfo
    {
        public uint instanceId;
        public uint typeId;
        public uint jsonPathHash;
        public uint dataSize;
        public long RSZOffset;

        public uint InstanceId => instanceId;
        public uint TypeId => typeId;
        public string? ClassName { get; set; }

        protected override bool DoRead(FileHandler handler)
        {
            handler.Read(ref instanceId);
            handler.Read(ref typeId);
            handler.Read(ref jsonPathHash);
            handler.Read(ref dataSize);
            handler.Read(ref RSZOffset);
            return true;
        }

        protected override bool DoWrite(FileHandler handler)
        {
            handler.Write(instanceId);
            handler.Write(typeId);
            handler.Write(jsonPathHash);
            handler.Write(dataSize);
            handler.Write(RSZOffset);
            return true;
        }

        public void ReadClassName(RszParser parser)
        {
            ClassName = parser.GetRSZClassName(typeId);
        }
    }
}
