using System.Reflection;
using System.Runtime.CompilerServices;
using RszTool.Common;

namespace RszTool
{
    public interface IOffsetField
    {
        int Offset { get; set; }
        bool Read(FileHandler handler);
        bool Write(FileHandler handler);

        object ObejctValue { get; }
    }


    public struct OffsetField<T> : IOffsetField where T : struct
    {
        public T Value { get; set; }
        public int Offset { get; set; }

        public readonly object ObejctValue => Value;
        public static implicit operator T(OffsetField<T> field) => field.Value;

        public bool Read(FileHandler handler)
        {
            Value = handler.Read<T>();
            return true;
        }

        public readonly bool Write(FileHandler handler)
        {
            return handler.Write(Value);
        }
    }


    public interface IModel
    {
        long Start { get; }
        bool Read(RszFileHandler handler, long start);
        bool Write(FileHandler handler);
    }


    public abstract class AdaptiveModel : IModel
    {
        public long Start { get; private set; }
        protected static readonly Stack<FileHandler> FileHandlers = new();
        public SortedDictionary<string, IOffsetField> Fields { get; set; } = new();

        protected void StartRead(FileHandler handler)
        {
            Start = handler.FTell();
            FileHandlers.Push(handler);
        }

        protected static void EndRead()
        {
            FileHandlers.Pop();
        }

        protected bool ReadField<T>(ref OffsetField<T> field) where T : struct
        {
            return ReadField(FileHandlers.Peek(), ref field);
        }

        protected bool ReadField<T>(FileHandler handler, ref OffsetField<T> field) where T : struct
        {
            field.Offset = (int)(handler.FTell() - Start);
            field.Value = handler.Read<T>();
            return true;
        }

        protected bool WriteField<T>(FileHandler handler, in OffsetField<T> field) where T : struct
        {
            field.Write(handler);
            return true;
        }

        public abstract bool Read(RszFileHandler handler, long start);

        public bool Write(FileHandler handler)
        {
            long start = handler.FTell();
            foreach (var fieldInfo in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var value = fieldInfo.GetValue(this);
                if (value is IOffsetField field)
                {
                    handler.FSeek(start + field.Offset);
                    if (!field.Write(handler))
                    {
                        Console.Error.WriteLine($"{this} Write {fieldInfo.Name} failed");
                        return false;
                    }
                }
            }
            return true;
        }
    }


    public class NamedOffsetField
    {
        public NamedOffsetField(string name, IOffsetField field)
        {
            Name = name;
            Field = field;
        }

        public string Name { get; set; }
        public IOffsetField Field { get; set; }
    }


    public class DynamicModel : IModel
    {
        public long Start { get; private set; }
        public List<NamedOffsetField>? Fields { get; }

        public bool Read(RszFileHandler handler, long start)
        {
            if (Fields == null) return false;
            Start = start;
            foreach (var item in Fields)
            {
                if (!item.Field.Write(handler))
                {
                    item.Field.Offset = (int)(handler.FTell() - Start);
                    item.Field.Read(handler);
                }
            }
            return true;
        }

        public bool Write(FileHandler handler)
        {
            if (Fields == null) return false;
            long start = handler.FTell();
            foreach (var item in Fields)
            {
                handler.FSeek(start + item.Field.Offset);
                if (!item.Field.Write(handler))
                {
                    Console.Error.WriteLine($"{this} Write {item.Name} failed");
                    return false;
                }
            }
            return true;
        }
    }
}
