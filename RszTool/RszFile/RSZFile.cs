using System.Text;
using RszTool.Common;

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

        public const uint Magic = 0x5a5352;

        protected override bool DoRead()
        {
            var handler = FileHandler;
            handler.Seek(0);
            if (!Header.Read(handler)) return false;
            if (Header.Data.magic != Magic)
            {
                throw new InvalidDataException($"Not a RSZ data");
            }

            var rszParser = RszParser;

            ObjectTableList.Read(handler, Header.Data.objectCount);

            handler.Seek(Header.Data.instanceOffset);
            for (int i = 0; i < Header.Data.instanceCount; i++)
            {
                InstanceInfo instanceInfo = new();
                instanceInfo.Read(handler);
                instanceInfo.ReadClassName(rszParser);
                InstanceInfoList.Add(instanceInfo);
            }

            handler.Seek(Header.Data.userdataOffset);
            Dictionary<int, int> instanceIdToUserData = new();
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
                        instanceIdToUserData[rszUserDataInfo.instanceId] = i;

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
                    instanceIdToUserData[rszUserDataInfo.instanceId] = i;
                }
            }

            // read instance data
            handler.Seek(Header.Data.dataOffset);
            for (int i = 0; i < InstanceInfoList.Count; i++)
            {
                RszClass? rszClass = RszParser.GetRSZClass(InstanceInfoList[i].typeId);
                if (rszClass == null)
                {
                    // throw new Exception($"RszClass {InstanceInfoList[i].typeId} not found!");
                    Console.Error.WriteLine($"RszClass {InstanceInfoList[i].typeId} not found!");
                    continue;
                }
                if (!instanceIdToUserData.TryGetValue(i, out int userDataIdx))
                {
                    userDataIdx = -1;
                }
                RszInstance instance = new(rszClass, i, userDataIdx != -1 ?
                    RSZUserDataInfoList[userDataIdx] : null);
                if (instance.RSZUserData == null)
                {
                    instance.Read(handler);
                }
                InstanceList.Add(instance);
            }

            // 正常的数据，被依赖的实例排在前面，但为了兼容手动改过的数据，还是先读取数据，再Unflatten
            InstanceListUnflatten();

            for (int i = 0; i < ObjectTableList.Count; i++)
            {
                StructModel<ObjectTable>? item = ObjectTableList[i];
                InstanceList[item.Data.instanceId].ObjectTableIndex = i;
            }
            return true;
        }

        protected override bool DoWrite()
        {
            var handler = FileHandler;
            ref var header = ref Header.Data;
            handler.Seek(Header.Size);
            ObjectTableList.Write(handler);

            handler.Align(16);
            header.instanceOffset = handler.Tell();
            InstanceInfoList.Write(handler);

            handler.Align(16);
            header.userdataOffset = handler.Tell();
            RSZUserDataInfoList.Write(handler);

            handler.Align(16);
            handler.StringTableFlush();

            // embedded userdata
            if (Option.TdbVersion <= 67 && EmbeddedRSZFileList != null)
            {
                for (int i = 0; i < RSZUserDataInfoList.Count; i++)
                {
                    handler.Align(16);
                    var item = (RSZUserDataInfo_TDB_LE_67)RSZUserDataInfoList[i];
                    item.RSZOffset = handler.Tell();
                    var embeddedRSZ = EmbeddedRSZFileList[i];
                    embeddedRSZ.WriteTo(FileHandler.WithOffset(item.RSZOffset));
                    // rewrite
                    item.dataSize = (uint)embeddedRSZ.Size;
                    item.Rewrite(handler);
                }
            }

            handler.Align(16);
            header.dataOffset = handler.Tell();
            InstanceList.Write(handler);

            header.magic = Magic;
            header.objectCount = ObjectTableList.Count;
            header.instanceCount = InstanceList.Count;
            header.userdataCount = RSZUserDataInfoList.Count;
            Header.Write(handler, 0);
            return true;
        }

        // 下面的函数用于重新构建数据，基本读写功能都在上面

        public RszInstance GetGameObject(int objectIndex)
        {
            return InstanceList[ObjectTableList[objectIndex].Data.instanceId];
        }

        public string InstanceStringify(RszInstance instance)
        {
            StringBuilder sb = new();
            sb.AppendLine(instance.Stringify(InstanceList));
            return sb.ToString();
        }

        public string ObjectsStringify()
        {
            StringBuilder sb = new();
            foreach (var item in ObjectTableList)
            {
                RszInstance instance = InstanceList[item.Data.instanceId];
                sb.AppendLine(instance.Stringify(InstanceList));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 检查实例索引正确，如果不正确，替换成正确序号，如果不在实例列表中，插入到列表
        /// </summary>
        /// <param name="instance"></param>
        public void FixInstanceIndex(RszInstance instance)
        {
            if (instance.Index < 0 || instance.Index >= InstanceList.Count || InstanceList[instance.Index] != instance)
            {
                // TODO 确保依赖的instance序号应该比自身小
                instance.Index = InstanceList.GetIndexOrAdd(instance);
            }
        }

        /// <summary>
        /// 检查实例索引正确，如果不正确，替换成正确序号，如果不在实例列表中，插入到列表
        /// </summary>
        /// <param name="instance"></param>
        public void FixInstanceIndexRecurse(RszInstance instance)
        {
            if (instance.RSZUserData == null)
            {
                var fields = instance.RszClass.fields;
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (field.IsReference)
                    {
                        if (field.array)
                        {
                            var items = (List<object>)instance.Values[i];
                            for (int j = 0; j < items.Count; j++)
                            {
                                if (items[j] is RszInstance instanceValue)
                                {
                                    FixInstanceIndexRecurse(instanceValue);
                                }
                            }
                        }
                        else if (instance.Values[i] is RszInstance instanceValue)
                        {
                            FixInstanceIndexRecurse(instanceValue);
                        }
                    }
                }
            }
            FixInstanceIndex(instance);
            if (instance.RSZUserData != null && instance.RSZUserData.InstanceId != instance.Index)
            {
                instance.RSZUserData.InstanceId = instance.Index;
            }
        }

        /// <summary>
        /// 实例的字段值，如果是对象序号，替换成对应的实例对象
        /// </summary>
        /// <param name="instance"></param>
        public void InstanceUnflatten(RszInstance instance)
        {
            if (instance.RSZUserData != null) return;
            var fields = instance.RszClass.fields;
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.IsReference)
                {
                    if (field.array)
                    {
                        var items = (List<object>)instance.Values[i];
                        for (int j = 0; j < items.Count; j++)
                        {
                            if (items[j] is int objectId)
                            {
                                items[j] = InstanceList[objectId];
                                InstanceUnflatten(InstanceList[objectId]);
                            }
                        }
                    }
                    else if (instance.Values[i] is int objectId)
                    {
                        instance.Values[i] = InstanceList[objectId];
                        InstanceUnflatten(InstanceList[objectId]);
                    }
                }
            }
        }

        /// <summary>
        /// 所有实例的字段值，如果是对象序号，替换成对应的实例对象
        /// </summary>
        public void InstanceListUnflatten()
        {
            foreach (var instance in InstanceList)
            {
                InstanceUnflatten(instance);
            }
        }

        /// <summary>
        /// 所有实例的字段值，如果是对象序号，替换成对应的实例对象
        /// </summary>
        public void InstanceListUnflatten(IEnumerable<RszInstance> list)
        {
            foreach (var instance in list)
            {
                InstanceUnflatten(instance);
            }
        }

        /// <summary>
        /// 所有实例的字段值，如果是对象，替换成实例序号，实例自身的Index也会修正
        /// </summary>
        public void FixInstanceListIndex()
        {
            FixInstanceListIndex(InstanceList);
        }

        /// <summary>
        /// 所有实例的字段值，如果是对象，替换成实例序号，实例自身的Index也会修正
        /// </summary>
        public void FixInstanceListIndex(IEnumerable<RszInstance> list)
        {
            foreach (var instance in list)
            {
                FixInstanceIndexRecurse(instance);
            }
            // TODO 确保依赖的instance序号应该比自身小
        }

        /// <summary>
        /// 根据实例列表，重建InstanceInfo
        /// </summary>
        /// <param name="flatten">是否先进行flatten</param>
        public void RebuildInstanceInfo(bool fixIndex = true, bool rebuildObjectTable = true)
        {
            if (fixIndex)
            {
                FixInstanceListIndex();
            }
            InstanceInfoList.Clear();
            RSZUserDataInfoList.Clear();
            List<RszInstance> instanceInTable = new();
            foreach (var instance in InstanceList)
            {
                InstanceInfoList.Add(new InstanceInfo
                {
                    typeId = instance.RszClass.typeId,
                    CRC = instance.RszClass.crc
                });
                if (instance.RSZUserData != null)
                {
                    RSZUserDataInfoList.Add(instance.RSZUserData);
                }
                if (rebuildObjectTable && instance.ObjectTableIndex != -1)
                {
                    instanceInTable.Add(instance);
                }
            }
            if (rebuildObjectTable)
            {
                // Rebuild ObjectTableList
                ObjectTableList.Clear();
                instanceInTable.Sort((a, b) => a.ObjectTableIndex.CompareTo(b.ObjectTableIndex));
                for (int i = 0; i < instanceInTable.Count; i++)
                {
                    RszInstance instance = instanceInTable[i];
                    instance.ObjectTableIndex = i;
                    var objectTableInfo = new StructModel<ObjectTable>();
                    objectTableInfo.Data.instanceId = instance.Index;
                    ObjectTableList.Add(objectTableInfo);
                }
            }
        }

        /// <summary>
        /// 添加到ObjectTable，会自动修正instance.ObjectTableIndex
        /// </summary>
        /// <param name="instance"></param>
        public void AddToObjectTable(RszInstance instance)
        {
            if (instance.ObjectTableIndex >= 0 && instance.ObjectTableIndex < ObjectTableList.Count &&
                ObjectTableList[instance.ObjectTableIndex].Data.instanceId == instance.Index)
            {
                return;
            }
            var objectTableItem = new StructModel<ObjectTable>();
            FixInstanceIndex(instance);
            objectTableItem.Data.instanceId = instance.Index;
            instance.ObjectTableIndex = ObjectTableList.Count;
            ObjectTableList.Add(objectTableItem);
        }

        /// <summary>
        /// 导入实例
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="clone">是否克隆instance</param>
        /// <param name="addToObjectTable">添加到ObjectTable</param>
        /// <returns>导入的副本列表，如果clone为false，返回null</returns>
        public void ImportInstance(RszInstance instance, bool clone = true, bool addToObjectTable = false)
        {
            instance = clone ? (RszInstance)instance.Clone() : instance;
            if (!clone && InstanceList.Contains(instance))
            {
                throw new InvalidOperationException("instance already in InstanceList");
            }
            int start = InstanceList.Count;
            FixInstanceIndexRecurse(instance);
            for (int i = start; i < InstanceList.Count; i++)
            {
                var newInstance = InstanceList[i];
                InstanceInfoList.Add(new InstanceInfo
                {
                    typeId = newInstance.RszClass.typeId,
                    CRC = newInstance.RszClass.crc
                });
                if (newInstance.RSZUserData != null)
                {
                    RSZUserDataInfoList.Add(newInstance.RSZUserData);
                }
                if (addToObjectTable)
                {
                    newInstance.ObjectTableIndex = ObjectTableList.Count;
                    var objectTableInfo = new StructModel<ObjectTable>();
                    objectTableInfo.Data.instanceId = newInstance.Index;
                    ObjectTableList.Add(objectTableInfo);
                }
            }
        }

        /// <summary>
        /// 导入实例
        /// </summary>
        /// <param name="instances"></param>
        /// <param name="addToObjectTable"></param>
        /// <returns>导入的副本列表，如果clone为false，返回null</returns>
        public void ImportInstances(IEnumerable<RszInstance> instances, bool clone = true, bool addToObjectTable = false)
        {
            foreach (var instance in instances)
            {
                ImportInstance(instance, clone, addToObjectTable);
            }
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


    public interface IRSZUserDataInfo : IModel, ICloneable
    {
        int InstanceId { get; set; }
        uint TypeId { get; }
        string? ClassName { get; }
    }


    public class RSZUserDataInfo : BaseModel, IRSZUserDataInfo
    {
        public int instanceId;
        public uint typeId;
        public ulong pathOffset;
        public string? path;

        public int InstanceId { get => instanceId; set => instanceId = value; }
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
            handler.StringTableAdd(path);
            handler.Write(pathOffset);
            return true;
        }

        public void ReadClassName(RszParser parser)
        {
            ClassName = parser.GetRSZClassName(typeId);
        }

        public UserdataInfo ToUserdataInfo(RszParser parser)
        {
            UserdataInfo info = new()
            {
                typeId = typeId,
                path = path,
                CRC = parser.GetRSZClassCRC(typeId),
            };
            return info;
        }
    }


    public class RSZUserDataInfo_TDB_LE_67 : BaseModel, IRSZUserDataInfo
    {
        public int instanceId;
        public uint typeId;
        public uint jsonPathHash;
        public uint dataSize;
        public long RSZOffset;

        public int InstanceId { get => instanceId; set => instanceId = value; }
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
