using System.Drawing;
using System.Numerics;

namespace RszTool
{
    /// <summary>
    /// 存放RszClass的数据
    /// </summary>
    public class RszInstance : BaseModel
    {
        public RszClass RszClass { get; set; }
        public object[] Values { get; set; }
        public int Index { get; set; }
        public int RSZUserDataIdx { get; set; }
        public string Name { get; set; }
        public IRSZUserDataInfo? RSZUserData { get; set; }

        public RszInstance(RszClass rszClass, int index, int rszUserDataIdx)
        {
            RszClass = rszClass;
            Values = new object[rszClass.fields.Length];
            Index = index;
            RSZUserDataIdx = rszUserDataIdx;
            Name = $"{rszClass.name}[{index}]";
        }

        /// <summary>
        /// 读取字段
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public override bool Read(FileHandler handler)
        {
            if (!base.Read(handler)) return false;
            for (int i = 0; i < RszClass.fields.Length; i++)
            {
                Values[i] = ReadRszField(handler, i);
            }
            EndRead(handler);
            return true;
        }

        public object ReadRszField(FileHandler handler, int index)
        {
            RszField field = RszClass.fields[index];
            handler.Align(field.array ? 4 : field.align);
            if (index == 0)
            {
                Start = handler.FTell();
            }
            if (field.array)
            {
                int count = handler.ReadInt();
                if (count > 1024)
                {
                    throw new InvalidDataException($"{field.name} count {count} too large");
                }
                List<object> arrayItems = new(count);
                for (int i = 0; i < count; i++)
                {
                    arrayItems.Add(ReadNormalField(handler, field));
                }
                return arrayItems;
            }
            else
            {
                return ReadNormalField(handler, field);
            }
        }

        public static object ReadNormalField(FileHandler handler, RszField field)
        {
            if (field.type == "String" || field.type == "Resource")
            {
                int charCount = handler.ReadInt();
                long stringStart = handler.FTell();
                string value = handler.ReadWString(charCount);
                handler.FSeek(stringStart + charCount * 2);
                // TODO checkOpenResource
                return value;
            }
            else
            {
                BinaryReader reader = handler.Reader;
                long startPos = handler.FTell();
                object value = field.type switch
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
                    "Color" => handler.Read<Color>(),
                    "Data" => handler.ReadBytes(field.size),
                    _ => throw new InvalidDataException($"Not support type {field.type}"),
                };
                handler.FSeek(startPos + field.size);
                return value;
            }
        }

        public bool WriteRszField(FileHandler handler, int index)
        {
            RszField field = RszClass.fields[index];
            handler.Align(field.array ? 4 : field.align);
            if (field.array)
            {
                foreach (var item in (object[])Values[index])
                {
                    WriteNormalField(handler, field, item);
                }
                return true;
            }
            else
            {
                return WriteNormalField(handler, field, Values[index]);
            }
        }

        public static bool WriteNormalField(FileHandler handler, RszField field, object value)
        {
            if (field.type == "String" || field.type == "Resource")
            {
                string valueStr = (string)value;
                int charCount = valueStr.Length;
                return handler.Write(charCount * 2) && handler.WriteWString(valueStr);
            }
            else
            {
                long startPos = handler.FTell();
                _ = field.type switch
                {
                    "Object" or "UserData" or "U32" => handler.Write((uint)value),
                    "S32" => handler.Write((int)value),
                    "U64" => handler.Write((ulong)value),
                    "S64" => handler.Write((long)value),
                    "Bool" => handler.Write((bool)value),
                    "F32" => handler.Write((float)value),
                    "F64" => handler.Write((double)value),
                    "Vec2" => handler.Write((Vector2)value),
                    "Vec3" => handler.Write((Vector3)value),
                    "Vec4" => handler.Write((Vector4)value),
                    "OBB" => handler.WriteArray((float[])value),
                    "Guid" => handler.Write((Guid)value),
                    "Color" => handler.Write((Color)value),
                    "Data" => handler.WriteBytes((byte[])value),
                    _ => throw new InvalidDataException($"Not support type {field.type}"),
                };
                handler.FSeek(startPos + field.size);
                return true;
            }
        }
    }
}