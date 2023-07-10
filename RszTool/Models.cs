using System.Runtime.InteropServices;

namespace RszTool
{
    public enum FieldType
    {
        Native,
        Object,
        Array,
        Error,
    }

    public abstract class DataType
    {
        public abstract int Size { get; }
    }

    public class NativeType : DataType
    {
        public NativeType(Type type)
        {
            Type = type;
        }

        public Type Type { get; set; }

        public override int Size
        {
            get
            {
                return Marshal.SizeOf(Type);
            }
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }

    public class ObjectType : DataType
    {
        public ObjectType(DataClass @class)
        {
            Class = @class;
        }

        public DataClass Class { get; set; }

        public override int Size => Class.Size;

        public override string ToString()
        {
            return Class.Name;
        }
    }

    public class ArrayType : DataType
    {
        public ArrayType(DataType elementType, int length)
        {
            ElementType = elementType;
            Length = length;
        }

        public int Length { get; set; }
        public DataType ElementType { get; set; }

        public override int Size => ElementType.Size * Length;

        public override string ToString()
        {
            return $"{ElementType}[{Length}]";
        }
    }

    public class DataClass
    {
        public DataClass(string name = "")
        {
            Name = name;
        }

        public string Name { get; set; } = "";
        public List<DataField> Fields { get; } = new();
        public int Size { get; set; }

        public DataField? GetFiled(string name)
        {
            return Fields.FirstOrDefault(f => f.Name == name);
        }

        public DataClass AddField(DataField field)
        {
            if (field.Offset == 0)
            {
                field.Offset = Size;
            }
            int size = field.Size;
            if (field.Offset + size >= Size)
            {
                Size = field.Offset + size;
            }
            Fields.Add(field);
            return this;
        }

        public DataClass AddField<T>(string name, int offset = 0, int align = 0)
        {
            var type = typeof(T);
            if (offset == 0)
            {
                offset = Size;
                if (align != 0)
                {
                    offset = Utils.AlignSize(offset, align);
                }
            }
            var field = new DataField(name, Fields.Count, offset, new NativeType(type));
            AddField(field);
            return this;
        }

        public DataClass AddArrayField(string name, DataType elementType, int length = 0, int offset = 0)
        {
            var field = new DataField(name, Fields.Count, offset, new ArrayType(elementType, length));
            AddField(field);
            return this;
        }

        public DataClass AddObjectField(string name, DataClass @class, int offset = 0)
        {
            var field = new DataField(name, Fields.Count, offset, new ObjectType(@class));
            AddField(field);
            return this;
        }
    }

    public interface IDataContainer
    {
        long Start { get; }
        string Name { get; }
        FileHandler? Handler { get; }

        object GetValue(int index);
        void SetValue(int index, object value);

        IEnumerable<(DataField, object)> IterData();
    }

    public class DataField
    {
        public DataField(string name, int index, int offset, DataType type)
        {
            Name = name;
            Index = index;
            Offset = offset;
            Type = type;
        }

        public string Name { get; set; } = "";
        public int Index { get; set; }
        public int Offset { get; set; }
        public DataType Type { get; set; }
        public int Size => Type.Size;
        public FieldType FieldType
        {
            get
            {
                if (Type is NativeType)
                    return FieldType.Native;
                else if (Type is ObjectType)
                    return FieldType.Object;
                else if (Type is ArrayType)
                    return FieldType.Array;
                return FieldType.Error;
            }
        }

        public long GetAddress(IDataContainer instance)
        {
            return instance.Start + Offset;
        }

        public virtual object? ReadValue(IDataContainer instance)
        {
            var handler = instance.Handler;
            if (handler != null)
            {
                long address = GetAddress(instance);
                handler.FSeek(address);
                if (Type is NativeType nativeType)
                {
                    Type dataType = nativeType.Type;
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
                else if (Type is ObjectType objectType)
                {
                    var value = instance.GetValue(Index);
                    if (value == null)
                    {
                        value = new DataObject(Name, address, objectType.Class, handler);
                    }
                    if (value is DataObject dataObject)
                    {
                        dataObject.ReadValues();
                    }
                    else
                    {
                        throw new ApplicationException("value is not a DataObject");
                    }
                    return value;
                }
                else if (Type is ArrayType arrayType)
                {
                    var value = instance.GetValue(Index);
                    if (value == null)
                    {
                        value = new DataArray(Name, address, arrayType, handler);
                    }
                    if (value is DataArray dataArray)
                    {
                        dataArray.ReadValues();
                    }
                    else
                    {
                        throw new ApplicationException("value is not a DataArray");
                    }
                    return value;
                }
            }
            else
            {
                throw new ApplicationException("handler is null");
            }
            return null;
        }

        public virtual bool WriteValue(IDataContainer instance, object value)
        {
            var handler = instance.Handler;
            if (handler != null)
            {
                handler.FSeek(GetAddress(instance));
                if (Type is NativeType nativeType)
                {
                    Type dataType = nativeType.Type;
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
                else if (Type is ObjectType objectType)
                {
                    if (value is DataObject dataObject)
                    {
                        dataObject.WriteValues();
                    }
                    else
                    {
                        throw new ApplicationException("value is not a DataObject");
                    }
                    return true;
                }
                else if (Type is ArrayType arrayType)
                {
                    if (value is DataArray dataArray)
                    {
                        dataArray.WriteValues();
                    }
                    else
                    {
                        throw new ApplicationException("value is not a DataArray");
                    }
                    return true;
                }
            }
            else
            {
                throw new ApplicationException("handler is null");
            }
            return false;
        }
    }

    public class DataObject : IDataContainer
    {
        public DataObject(string? name, long start, DataClass cls, FileHandler handler, object[]? datas = null)
        {
            Name = name ?? cls.Name;
            Start = start;
            Class = cls ?? new DataClass(Name);
            Datas = datas ?? new object[Class.Fields.Count];
            HandlerRef = new WeakReference<FileHandler>(handler);
        }

        public string Name { get; set; }
        public long Start { get; set; }
        public DataClass Class { get; set; }
        object[] Datas { get; set; }
        public List<DataField> Fields => Class.Fields;
        public WeakReference<FileHandler>? HandlerRef { get; set; }
        public FileHandler? Handler => HandlerRef?.GetTarget();

        public int FieldIndex(string name)
        {
            var fields = Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        public object GetValue(int index)
        {
            return Datas[index];
        }

        public object? GetValue(string name)
        {
            int index = FieldIndex(name);
            return index != -1 ? Datas[index] : null;
        }

        public void SetValue(int index, object value)
        {
            Datas[index] = value;
        }

        public void SetValue(string name, object value)
        {
            int index = FieldIndex(name);
            if (index != -1)
            {
                Datas[index] = value;
            }
        }

        public object? ReadValue(int index)
        {
            var fields = Fields;
            var value = fields[index].ReadValue(this);
            if (value == null)
            {
                throw new ApplicationException($"Read {Name}.{fields[index].Name} failed");
            }
            Datas[index] = value;
            return value;
        }

        public object? ReadValue(string name)
        {
            int index = FieldIndex(name);
            return index != -1 ? ReadValue(index) : null;
        }

        public void WriteValue(int index, object? value = null)
        {
            var fields = Fields;
            if (value != null)
            {
                Datas[index] = value;
            }
            fields[index].WriteValue(this, Datas[index]);
        }

        public void WriteValue(string name, object? value = null)
        {
            int index = FieldIndex(name);
            if (index != -1)
            {
                WriteValue(index, value);
            }
        }

        public void ReadValues()
        {
            var fields = Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                var value = fields[i].ReadValue(this);
                if (value == null)
                {
                    throw new ApplicationException($"Read {Name}.{fields[i].Name} failed");
                }
                Datas[i] = value;
            }
        }

        public void WriteValues()
        {
            var fields = Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                var value = Datas[i];
                if (value == null)
                {
                    throw new ApplicationException($"{Name}.{fields[i].Name} is null");
                }
                fields[i].WriteValue(this, value);
            }
        }

        public IEnumerable<(DataField, object)> IterData()
        {
            var fields = Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                yield return (fields[i], Datas[i]);
            }
        }
    }

    public class DataArray : IDataContainer
    {
        public DataArray(string name, long start, ArrayType arrayType, FileHandler handler, object[]? datas = null)
        {
            Name = name;
            Start = start;
            ArrayType = arrayType;
            Datas = datas ?? new object[ArrayType.Length];
            HandlerRef = new WeakReference<FileHandler>(handler);
            tempField = new(name, 0, 0, arrayType.ElementType);
        }

        public string Name { get; set; } = "";
        public long Start { get; set; }
        public ArrayType ArrayType { get; set; }
        object[] Datas { get; set; }
        public WeakReference<FileHandler>? HandlerRef { get; set; }
        public FileHandler? Handler => HandlerRef?.GetTarget();

        private DataField tempField;

        public object GetValue(int index)
        {
            return Datas[index];
        }

        public void SetValue(int index, object value)
        {
            Datas[index] = value;
        }

        public void ReadValues()
        {
            tempField.Offset = 0;
            for (int i = 0; i < ArrayType.Length; i++)
            {
                tempField.Index = i;
                object? value = tempField.ReadValue(this);
                if (value == null)
                {
                    throw new ApplicationException($"Read {Name}[{i}] failed");
                }
                Datas[i] = value;
                tempField.Offset += ArrayType.Size;
            }
        }

        public void WriteValues()
        {
            tempField.Offset = 0;
            for (int i = 0; i < ArrayType.Length; i++)
            {
                var value = Datas[i];
                if (value == null)
                {
                    throw new ApplicationException($"{Name}[{i}] is null");
                }
                tempField.Index = i;
                tempField.WriteValue(this, value);
                tempField.Offset += ArrayType.Size;
            }
        }

        public IEnumerable<(DataField, object)> IterData()
        {
            tempField.Offset = 0;
            for (int i = 0; i < ArrayType.Length; i++)
            {
                tempField.Index = i;
                yield return (tempField, Datas[i]);
                tempField.Offset += ArrayType.Size;
            }
        }
    }
}
