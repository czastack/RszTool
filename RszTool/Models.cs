using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RszTool
{
    public class DataClass
    {
        public string Name { get; set; } = "";
        public List<BaseDataField> Fields { get; } = new();
        public int Size { get; set; }

        public BaseDataField? GetFiled(string name)
        {
            return Fields.FirstOrDefault(f => f.Name == name);
        }

        public DataField AddField<T>(string name, int size = 0, int offset = 0, int align = 0)
        {
            var type = typeof(T);
            if (size == 0)
            {
                if (type.IsValueType)
                {
                    size = Unsafe.SizeOf<T>();
                }
                else
                {
                    throw new System.ArgumentException("Type is not value type, cannot get size");
                }
            }
            if (offset == 0)
            {
                offset = Size;
                if (align != 0)
                {
                    offset = Utils.AlignSize(offset, align);
                }
            }
            if (offset + size >= Size)
            {
                Size = offset + size;
            }
            var field = new DataField(name, size, offset, type);
            Fields.Add(field);
            return field;
        }

        public ArrayField AddArrayField(string name, BaseDataField baseField, int offset = 0, int length = 0)
        {
            var field = new ArrayField(name, offset, length, baseField);
            Fields.Add(field);
            return field;
        }

        public ClassField AddClassField(string name, DataClass @class, int offset = 0)
        {
            var field = new ClassField(name, offset, @class);
            Fields.Add(field);
            return field;
        }
    }

    public interface IDataContainer
    {
        long Start { get; }
        string Name { get; }
        RszFileHandler? Handler { get; }
    }

    public class DataObject : IDataContainer
    {
        public DataObject(string? name, long start, DataClass? cls)
        {
            Name = name ?? cls?.Name ?? "";
            Class = cls ?? new DataClass();
            Start = start;
        }

        public string Name { get; set; }
        public long Start { get; set; }
        public DataClass Class { get; set; }
        public List<BaseDataField> Fields => Class.Fields;
        public WeakReference<RszFileHandler>? HandlerRef { get; set; }

        public DataField AddField<T>(string name, T value, int size = 0, int offset = 0, int align = 0)
        {
            return Class.AddField<T>(name, size, offset);
        }

        public RszFileHandler? Handler => HandlerRef?.GetTarget();
    }

    public class DataArray : IDataContainer
    {
        public string Name { get; set; } = "";
        public long Start { get; set; }
        public WeakReference<RszFileHandler>? HandlerRef { get; set; }

        public RszFileHandler? Handler => HandlerRef?.GetTarget();
    }

    public enum DataType
    {
        Native,
        Object,
        Array,
    }

    public class BaseDataField
    {
        public BaseDataField(string name, int size, int offset)
        {
            Name = name;
            Size = size;
            Offset = offset;
        }

        public string Name { get; set; } = "";
        public int Size { get; set; }
        public int Offset { get; set; }

        public long GetAddress(DataObject instance)
        {
            return instance.Start + Offset;
        }
    }

    public class DataField : BaseDataField
    {
        public DataField(string name, int size, int offset, Type type)
            : base(name, size, offset)
        {
            Type = type;
        }

        public Type Type { get; set; }

        public virtual object? ReadValue(DataObject instance)
        {
            var handler = instance.Handler;
            if (handler != null)
            {
                handler.FSeek(GetAddress(instance));
                Type dataType = Type;
                BinaryReader reader = handler.Reader;
                if (dataType == typeof(int))
                    return reader.ReadInt32();
                else if (dataType == typeof(uint))
                    return reader.ReadUInt32();
                else if (dataType == typeof(byte))
                    return reader.ReadByte();
                else if (dataType == typeof(sbyte))
                    return reader.ReadSByte();
                else if (dataType == typeof(short))
                    return reader.ReadInt16();
                else if (dataType == typeof(ushort))
                    return reader.ReadUInt16();
                else if (dataType == typeof(long))
                    return reader.ReadInt64();
                else if (dataType == typeof(ulong))
                    return reader.ReadUInt64();
                else if (dataType == typeof(float))
                    return reader.ReadSingle();
                else if (dataType == typeof(double))
                    return reader.ReadDouble();
                else if (dataType == typeof(bool))
                    return reader.ReadBoolean();
                // else if (dataType == typeof(string))
                //     return reader.ReadString();
            }
            return null;
        }

        public virtual bool WriteValue(DataObject instance, object value)
        {
            var handler = instance.Handler;
            if (handler != null)
            {
                handler.FSeek(GetAddress(instance));
                Type dataType = Type;
                BinaryWriter writer = handler.Writer;
                if (dataType == typeof(int))
                    writer.Write(Convert.ToInt32(value));
                else if (dataType == typeof(uint))
                    writer.Write(Convert.ToUInt32(value));
                else if (dataType == typeof(byte))
                    writer.Write(Convert.ToByte(value));
                else if (dataType == typeof(sbyte))
                    writer.Write(Convert.ToSByte(value));
                else if (dataType == typeof(short))
                    writer.Write(Convert.ToInt16(value));
                else if (dataType == typeof(ushort))
                    writer.Write(Convert.ToUInt16(value));
                else if (dataType == typeof(long))
                    writer.Write(Convert.ToInt64(value));
                else if (dataType == typeof(ulong))
                    writer.Write(Convert.ToUInt64(value));
                else if (dataType == typeof(float))
                    writer.Write(Convert.ToSingle(value));
                else if (dataType == typeof(double))
                    writer.Write(Convert.ToDouble(value));
                else if (dataType == typeof(bool))
                    writer.Write(Convert.ToBoolean(value));
                // else if (dataType == typeof(string))
                //     writer.Write(value.ToString()!);
                else
                    return false;
                return true;
            }
            return false;
        }
    }

    public class ClassField : BaseDataField
    {
        public ClassField(string name, int offset, DataClass @class)
            : base(name, @class.Size, offset)
        {
            Class = @class;
        }

        public DataClass? Class { get; set; }
    }

    public class ArrayField : BaseDataField
    {
        public ArrayField(string name, int offset, int length, BaseDataField field)
            : base(name, field.Size * length, offset)
        {
            Length = length;
            Field = field;
        }

        public int Length { get; set; }
        public BaseDataField Field { get; set; }
    }


    /*
    public class DataField<T> : DataField where T : struct
    {
        public DataField(string name, int size, int offset) : base(name, size, offset, typeof(T))
        {
        }

        public override object? ReadValue(DataObject instance)
        {
            var handler = instance.Handler;
            if (handler != null)
            {
                Span<byte> buffer = stackalloc byte[Size];
                handler.FSeek(GetAddress(instance));
                handler.Reader.Read(buffer);
                return Utils.BytesAsStructure<T>(buffer);
            }
            return null;
        }

        public override bool WriteValue(DataObject instance, object value)
        {
            var handler = instance.Handler;
            if (handler != null)
            {
                T data = (T)Convert.ChangeType(value, typeof(T))!;
                Span<byte> buffer = Utils.StructureAsBytes(ref data);
                handler.FSeek(GetAddress(instance));
                handler.Writer.Write(buffer);
                return true;
            }
            return false;
        }
    } */
}
