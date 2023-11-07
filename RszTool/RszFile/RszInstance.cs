using System.Numerics;

namespace RszTool
{
    /// <summary>
    /// 存放RszClass的数据
    /// </summary>
    public class RszInstance : BaseModel
    {
        public RszClass RszClass { get; set; }
        public object[] Value { get; set; }
        public int Index { get; set; }
        public int RSZUserDataIdx { get; set; }
        public string Name { get; set; }
        public IRSZUserDataInfo? RSZUserData { get; set; }

        public RszInstance(RszClass rszClass, int index, int rszUserDataIdx)
        {
            RszClass = rszClass;
            Value = new object[rszClass.fields.Length];
            Index = index;
            RSZUserDataIdx = rszUserDataIdx;
            Name = $"{rszClass.name}[{index}]";
        }

        public object ReadRszFile(FileHandler handler, int index)
        {
            RszField field = RszClass.fields[index];
            handler.Align(field.array ? 4 : field.align);
            if (field.array)
            {
                int count = handler.ReadInt();
                if (count > 1024)
                {
                    throw new InvalidDataException($"{field.name} count {count} too large");
                }
                List<object> arrayItems = new(count);
                Value[index] = arrayItems;
                for (int i = 0; i < count; i++)
                {
                    arrayItems.Add(ReadRszFile(handler, index));
                }
                return arrayItems;
            }
            return null;
        }

        public object ReadArrayItem(FileHandler handler, RszField listField)
        {
            if (listField.type == "String" || listField.type == "Resource")
            {
                int charCount = handler.ReadInt();
                long stringStart = handler.FTell();
                string value = handler.ReadWString(charCount);
                handler.FSeek(stringStart + charCount * 2);
                return value;
            }
            else
            {
                BinaryReader reader = handler.Reader;
                long startPos = handler.FTell();
                object value = listField.type switch
                {
                    "Object" or "UserData" or "U32" => reader.ReadUInt32(),
                    "S32" => reader.ReadInt32(),
                    "U64" => reader.ReadUInt64(),
                    "S64" => reader.ReadInt64(),
                    "Bool" => reader.ReadBoolean(),
                    "F32" => reader.ReadSingle(),
                    "F64" => reader.ReadDouble(),
                    "Vec2" => handler.Read<Vector2>(),
                    "Vec3" => handler.Read<Vector3>(),
                    "Vec4" => handler.Read<Vector4>(),
                    "OBB" => handler.ReadArray<float>(20),
                    "Guid" => handler.Read<Guid>(),
                    _ => throw new InvalidDataException($"Not support type {listField.type}"),
                };
                handler.FSeek(startPos + listField.size);
                return value;
            }
        }
    }
}
