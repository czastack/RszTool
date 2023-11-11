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

        private void AlignFirstField(FileHandler handler)
        {
            handler.Align(RszClass.fields[0].array ? 4 : RszClass.fields[0].align);
        }

        /// <summary>
        /// 读取字段
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        protected override bool DoRead(FileHandler handler)
        {
            // if has RSZUserData, it is external
            if (RSZUserData != null || RszClass.fields.Length == 0) return true;

            AlignFirstField(handler);
            // Console.WriteLine($"read {Name} at: {handler.Position:X}");
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
            // Console.WriteLine($"    read at: {handler.Position:X} {field.original_type} {field.name}");
            if (field.array)
            {
                int count = handler.ReadInt();
                if (count < 0)
                {
                    throw new InvalidDataException($"{field.name} count {count} < 0");
                }
                if (count > 1024)
                {
                    throw new InvalidDataException($"{field.name} count {count} too large");
                }
                List<object> arrayItems = new(count);
                if (count > 0) handler.Align(field.align);
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
            if (field.type == TypeIDs.String || field.type == TypeIDs.Resource)
            {
                int charCount = handler.ReadInt();
                long stringStart = handler.Tell();
                string value = charCount <= 1 ? "" : handler.ReadWString(charCount: charCount);
                handler.Seek(stringStart + charCount * 2);
                // TODO checkOpenResource
                return value;
            }
            else
            {
                long startPos = handler.Tell();
                object value = field.type switch
                {
                    TypeIDs.S32 => handler.ReadInt(),
                    TypeIDs.Object or TypeIDs.UserData or TypeIDs.U32 => handler.ReadUInt(),
                    TypeIDs.S64 => handler.ReadInt64(),
                    TypeIDs.U64 => handler.ReadUInt64(),
                    TypeIDs.F32 => handler.ReadFloat(),
                    TypeIDs.F64 => handler.ReadDouble(),
                    TypeIDs.Bool => handler.ReadBoolean(),
                    TypeIDs.S8 => handler.ReadSByte(),
                    TypeIDs.U8 => handler.ReadByte(),
                    TypeIDs.S16 => handler.ReadShort(),
                    TypeIDs.U16 => handler.ReadUShort(),
                    TypeIDs.Vec2 or TypeIDs.Float2 => handler.Read<Vector2>(),
                    TypeIDs.Vec3 or TypeIDs.Float3 => handler.Read<Vector3>(),
                    TypeIDs.Vec4 or TypeIDs.Float4 => handler.Read<Vector4>(),
                    TypeIDs.OBB => handler.ReadArray<float>(20),
                    TypeIDs.Guid or TypeIDs.GameObjectRef => handler.Read<Guid>(),
                    TypeIDs.Color => handler.Read<Color>(),
                    TypeIDs.Range => handler.Read<Range>(),
                    TypeIDs.Quaternion => handler.Read<Quaternion>(),
                    TypeIDs.Data => handler.ReadBytes(field.size),
                    _ => throw new InvalidDataException($"Not support type {field.type}"),
                };
                /* if (field.size != handler.Tell() - startPos)
                {
                    throw new InvalidDataException($"{field.name} size not match");
                } */
                handler.Seek(startPos + field.size);
                return value;
            }
        }

        protected override bool DoWrite(FileHandler handler)
        {
            // if has RSZUserData, it is external
            if (RSZUserData != null || RszClass.fields.Length == 0) return true;
            AlignFirstField(handler);
            // Console.WriteLine($"write {Name} at: {(handler.Offset + handler.Tell()):X}");
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
            // Console.WriteLine($"    write at: {handler.Position:X} {field.original_type} {field.name}");
            if (field.array)
            {
                List<object> list = (List<object>)Values[index];
                handler.Write(list.Count);
                if (list.Count > 0)
                {
                    handler.Align(field.align);
                    foreach (var value in list)
                    {
                        WriteNormalField(handler, field, value);
                    }
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
            if (field.type == TypeIDs.String || field.type == TypeIDs.Resource)
            {
                string valueStr = (string)value;
                return handler.Write(valueStr.Length + 1) && handler.WriteWString(valueStr);
            }
            else
            {
                long startPos = handler.Tell();
                _ = field.type switch
                {
                    TypeIDs.S32 => handler.Write((int)value),
                    TypeIDs.Object or TypeIDs.UserData or TypeIDs.U32 => handler.Write((uint)value),
                    TypeIDs.S64 => handler.Write((long)value),
                    TypeIDs.U64 => handler.Write((ulong)value),
                    TypeIDs.F32 => handler.Write((float)value),
                    TypeIDs.F64 => handler.Write((double)value),
                    TypeIDs.Bool => handler.Write((bool)value),
                    TypeIDs.S8 => handler.Write((sbyte)value),
                    TypeIDs.U8 => handler.Write((byte)value),
                    TypeIDs.S16 => handler.Write((short)value),
                    TypeIDs.U16 => handler.Write((ushort)value),
                    TypeIDs.Vec2 or TypeIDs.Float2 => handler.Write((Vector2)value),
                    TypeIDs.Vec3 or TypeIDs.Float3 => handler.Write((Vector3)value),
                    TypeIDs.Vec4 or TypeIDs.Float4 => handler.Write((Vector4)value),
                    TypeIDs.OBB => handler.WriteArray((float[])value),
                    TypeIDs.Guid or TypeIDs.GameObjectRef => handler.Write((Guid)value),
                    TypeIDs.Color => handler.Write((Color)value),
                    TypeIDs.Range => handler.Write((Range)value),
                    TypeIDs.Quaternion => handler.Write((Quaternion)value),
                    TypeIDs.Data => handler.WriteBytes((byte[])value),
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
                TypeIDs.S32 => typeof(int),
                TypeIDs.Object or TypeIDs.UserData or TypeIDs.U32 => typeof(uint),
                TypeIDs.S64 => typeof(long),
                TypeIDs.U64 => typeof(ulong),
                TypeIDs.F32 => typeof(float),
                TypeIDs.F64 => typeof(double),
                TypeIDs.Bool => typeof(bool),
                TypeIDs.S8 => typeof(sbyte),
                TypeIDs.U8 => typeof(byte),
                TypeIDs.S16 => typeof(short),
                TypeIDs.U16 => typeof(ushort),
                TypeIDs.Vec2 or TypeIDs.Float2 => typeof(Vector2),
                TypeIDs.Vec3 or TypeIDs.Float3 => typeof(Vector3),
                TypeIDs.Vec4 or TypeIDs.Float4 => typeof(Vector4),
                TypeIDs.OBB => typeof(float[]),
                TypeIDs.Guid or TypeIDs.GameObjectRef => typeof(Guid),
                TypeIDs.Color => typeof(Color),
                TypeIDs.Range => typeof(Range),
                TypeIDs.Quaternion => typeof(Quaternion),
                TypeIDs.Data => typeof(byte[]),
                TypeIDs.String or TypeIDs.Resource => typeof(string),
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
            if (RSZUserData != null)
            {
            }
            else
            {
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
