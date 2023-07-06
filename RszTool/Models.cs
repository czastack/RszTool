using System.Runtime.CompilerServices;

namespace RszTool
{
    public class DataClass
    {
        public string Name { get; set; } = "";
        public List<DataField> Fields { get; } = new();
        public int Size { get; set; }

        public DataField? GetFiled(string name)
        {
            return Fields.FirstOrDefault(f => f.Name == name);
        }

        public void AddField<T>(string name, int size = 0, int offset = 0)
        {
            var type = typeof(T);
            if (type.IsValueType)
            {
                if (size == 0) {
                    size = Unsafe.SizeOf<T>();
                }
            }
            if (offset == 0)
            {
                offset = Size;
                Size += size;
            }
            Fields.Add(new DataField(name, size, offset, type));
        }
    }

    public class DataObject
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
        public List<DataField> Fields => Class.Fields;

        public void AddField<T>(string name, T value, int size = 0, int offset = 0)
        {
            Class.AddField<T>(name, size, offset);
        }
    }

    public class DataField
    {
        public DataField(string name, int size, int offset, Type type)
        {
            Name = name;
            Size = size;
            Offset = offset;
            Type = type;
        }

        public string Name { get; set; } = "";
        public int Size { get; set; }
        public int Offset { get; set; }
        public int Align { get; set; }
        public Type Type { get; set; }
    }
}
