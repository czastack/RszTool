using System.Drawing;
using System.Numerics;
using System.Text;

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
        protected override bool DoRead(FileHandler handler)
        {
            for (int i = 0; i < RszClass.fields.Length; i++)
            {
                Values[i] = ReadRszField(handler, i);
            }
            return true;
        }

        public object ReadRszField(FileHandler handler, int index)
        {
            RszField field = RszClass.fields[index];
            handler.Align(field.array ? 4 : field.align);
            if (index == 0)
            {
                // after align
                Start = handler.Tell();
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
                long stringStart = handler.Tell();
                string value = handler.ReadWString(charCount);
                handler.Seek(stringStart + charCount * 2);
                // TODO checkOpenResource
                return value;
            }
            else
            {
                long startPos = handler.Tell();
                object value = field.type switch
                {
                    "S32" => handler.ReadInt(),
                    "Object" or "UserData" or "U32" => handler.ReadUInt(),
                    "S64" => handler.ReadInt64(),
                    "U64" => handler.ReadUInt64(),
                    "F32" => handler.ReadFloat(),
                    "F64" => handler.ReadDouble(),
                    "Bool" => handler.ReadBoolean(),
                    "S8" => handler.ReadSByte(),
                    "U8" => handler.ReadByte(),
                    "S16" => handler.ReadShort(),
                    "U16" => handler.ReadUShort(),
                    "Vec2" => handler.Read<Vector2>(),
                    "Vec3" => handler.Read<Vector3>(),
                    "Vec4" => handler.Read<Vector4>(),
                    "OBB" => handler.ReadArray<float>(20),
                    "Guid" => handler.Read<Guid>(),
                    "Color" => handler.Read<Color>(),
                    "Data" => handler.ReadBytes(field.size),
                    _ => throw new InvalidDataException($"Not support type {field.type}"),
                };
                handler.Seek(startPos + field.size);
                return value;
            }
        }

        protected override bool DoWrite(FileHandler handler)
        {
            for (int i = 0; i < RszClass.fields.Length; i++)
            {
                WriteRszField(handler, i);
            }
            return true;
        }

        public bool WriteRszField(FileHandler handler, int index)
        {
            RszField field = RszClass.fields[index];
            handler.Align(field.array ? 4 : field.align);
            if (field.array)
            {
                List<object> list = (List<object>)Values[index];
                handler.Write(list.Count);
                foreach (var item in list)
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
                long startPos = handler.Tell();
                _ = field.type switch
                {
                    "S32" => handler.Write((int)value),
                    "Object" or "UserData" or "U32" => handler.Write((uint)value),
                    "S64" => handler.Write((long)value),
                    "U64" => handler.Write((ulong)value),
                    "F32" => handler.Write((float)value),
                    "F64" => handler.Write((double)value),
                    "Bool" => handler.Write((bool)value),
                    "S8" => handler.Write((sbyte)value),
                    "U8" => handler.Write((byte)value),
                    "S16" => handler.Write((short)value),
                    "U16" => handler.Write((ushort)value),
                    "Vec2" => handler.Write((Vector2)value),
                    "Vec3" => handler.Write((Vector3)value),
                    "Vec4" => handler.Write((Vector4)value),
                    "OBB" => handler.WriteArray((float[])value),
                    "Guid" => handler.Write((Guid)value),
                    "Color" => handler.Write((Color)value),
                    "Data" => handler.WriteBytes((byte[])value),
                    _ => throw new InvalidDataException($"Not support type {field.type}"),
                };
                handler.Seek(startPos + field.size);
                return true;
            }
        }

        public static Type FieldTypeToSharpType(RszField field)
        {
            return field.type switch
            {
                "S32" => typeof(int),
                "Object" or "UserData" or "U32" => typeof(uint),
                "S64" => typeof(long),
                "U64" => typeof(ulong),
                "F32" => typeof(float),
                "F64" => typeof(double),
                "Bool" => typeof(bool),
                "S8" => typeof(sbyte),
                "U8" => typeof(byte),
                "S16" => typeof(short),
                "U16" => typeof(ushort),
                "Vec2" => typeof(Vector2),
                "Vec3" => typeof(Vector3),
                "Vec4" => typeof(Vector4),
                "OBB" => typeof(float[]),
                "Guid" => typeof(Guid),
                "Color" => typeof(Color),
                "Data" => typeof(byte[]),
                "String" or "Resource" => typeof(string),
                _ => throw new InvalidDataException($"Not support type {field.type}"),
            };
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object? GetFieldValue(string name)
        {
            int index = RszClass.IndexOfField(name);
            if (index == -1) return null;
            return Values[index];
        }

        /// <summary>
        /// 设置字段值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetFieldValue(string name, object value)
        {
            int index = RszClass.IndexOfField(name);
            if (index == -1) return;
            Values[index] = value;
        }

        public void Stringify(StringBuilder sb)
        {
            if (RszClass.crc == 0)
            {
                sb.Append("NULL");
                return;
            }
            sb.Append(Name);
            sb.AppendLine(" {");
            for (int i = 0; i < RszClass.fields.Length; i++)
            {
                RszField field = RszClass.fields[i];
                sb.Append("    ");
                sb.Append($"{field.original_type} {field.name} = ");
                if (field.array)
                {
                    sb.Append('[');
                    foreach (var item in (List<object>)Values[i])
                    {
                        sb.Append(item);
                        sb.Append(", ");
                    }
                    sb.Length -= 2;
                    sb.AppendLine("];");
                }
                else
                {
                    sb.Append(Values[i]);
                    sb.AppendLine(";");
                }
            }
            sb.AppendLine("}");
        }

        public string Stringify()
        {
            StringBuilder sb = new();
            Stringify(sb);
            return sb.ToString();
        }
    }
}
