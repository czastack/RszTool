using System.Drawing;
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
        public OffsetField()
        {
        }

        public T Value { get; set; }
        public int Offset { get; set; } = -1;

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
        long Start { get; set; }
        long Size { get; }
        bool Read(FileHandler handler);
        bool Write(FileHandler handler);
    }


    public static class IModelExtensions
    {
        public static bool Read(this IModel model, FileHandler handler, long start, bool jumpBack = true)
        {
            long pos = handler.Tell();
            if (start != -1)
            {
                handler.Seek(start);
            }
            bool result = model.Read(handler);
            if (jumpBack) handler.Seek(pos);
            return result;
        }

        public static bool Read<T>(this List<T> list, FileHandler handler, int count) where T : IModel, new()
        {
            for (int i = 0; i < count; i++)
            {
                T item = new();
                if (!item.Read(handler)) return false;
                list.Add(item);
            }
            return true;
        }

        public static bool Write(this IModel model, FileHandler handler, long start, bool jumpBack = true)
        {
            long pos = handler.Tell();
            if (start != -1)
            {
                handler.Seek(start);
            }
            bool result = model.Write(handler);
            if (jumpBack) handler.Seek(pos);
            return result;
        }

        public static bool Rewrite(this IModel model, FileHandler handler, bool jumpBack = true)
        {
            return Write(model, handler, model.Start, jumpBack);
        }

        public static bool Write(this IEnumerable<IModel> list, FileHandler handler)
        {
            foreach (var item in list)
            {
                if (!item.Write(handler)) return false;
            }
            return true;
        }

        public static bool Rewrite(this IEnumerable<IModel> list, FileHandler handler)
        {
            foreach (var item in list)
            {
                if (!item.Rewrite(handler)) return false;
            }
            return true;
        }
    }


    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ModelAutoOffsetFieldAttribute : Attribute
    {
    }

    public abstract class AdaptiveModel : IModel
    {
        public long Start { get; set; } = -1;
        public long Size { get; private set; }
        protected static readonly Stack<FileHandler> FileHandlers = new();

        protected KeyValuePair<string, IOffsetField>[]? fields;
        public KeyValuePair<string, IOffsetField>[] Fields =>
            fields ??= (from fieldInfo in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        let field = fieldInfo.GetValue(this)
                        where field is IOffsetField
                        select new KeyValuePair<string, IOffsetField>(fieldInfo.Name, (IOffsetField)field)).ToArray();

        protected void StartRead(FileHandler handler)
        {
            Start = handler.Tell();
            FileHandlers.Push(handler);
        }

        protected void EndRead()
        {
            Size = FileHandlers.Peek().Tell() - Start;
            FileHandlers.Pop();
        }

        protected bool ReadField<T>(ref OffsetField<T> field) where T : struct
        {
            return ReadField(FileHandlers.Peek(), ref field);
        }

        protected bool ReadField<T>(FileHandler handler, ref OffsetField<T> field) where T : struct
        {
            field.Offset = (int)(handler.Tell() - Start);
            field.Value = handler.Read<T>();
            return true;
        }

        protected bool WriteField<T>(FileHandler handler, in OffsetField<T> field) where T : struct
        {
            if (field.Offset == -1) return false;
            long start = handler.Tell();
            handler.Seek(start + field.Offset);
            field.Write(handler);
            return true;
        }

        public abstract bool Read(FileHandler handler);

        public bool Write(FileHandler handler)
        {
            Start = handler.Tell();
            foreach (var (name, field) in Fields)
            {
                if (field.Offset == -1) continue;
                handler.Seek(Start + field.Offset);
                if (!field.Write(handler))
                {
                    Console.Error.WriteLine($"{this} Write {name} failed");
                    return false;
                }
            }
            Size = handler.Tell() - Start;
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
        public long Start { get; set; } = -1;
        public long Size { get; private set; }
        public List<NamedOffsetField>? Fields { get; }

        public bool Read(FileHandler handler)
        {
            if (Fields == null) return false;
            Start = handler.Tell();
            foreach (var item in Fields)
            {
                if (!item.Field.Write(handler))
                {
                    item.Field.Offset = (int)(handler.Tell() - Start);
                    item.Field.Read(handler);
                }
            }
            Size = handler.Tell() - Start;
            return true;
        }

        public bool Write(FileHandler handler)
        {
            if (Fields == null) return false;
            Start = handler.Tell();
            foreach (var item in Fields)
            {
                handler.Seek(Start + item.Field.Offset);
                if (!item.Field.Write(handler))
                {
                    Console.Error.WriteLine($"{this} Write {item.Name} failed");
                    return false;
                }
            }
            Size = handler.Tell() - Start;
            return true;
        }
    }


    public class StructModel<T> : IModel where T : struct
    {
        public T Data = default;
        public long Start { get; set; } = -1;
        public long Size => Unsafe.SizeOf<T>();

        public bool Read(FileHandler handler)
        {
            Start = handler.Tell();
            handler.Read(ref Data);
            return true;
        }

        public bool Write(FileHandler handler)
        {
            Start = handler.Tell();
            return handler.Write(ref Data);
        }
    }


    public abstract class BaseModel : IModel
    {
        public long Start { get; set; }
        public long Size { get; protected set; }

        public bool Read(FileHandler handler)
        {
            handler.CheckRange();
            Start = handler.Tell();
            bool result = DoRead(handler);
            Size = handler.Tell() - Start;
            return result;
        }

        public bool Write(FileHandler handler)
        {
            Start = handler.Tell();
            bool result = DoWrite(handler);
            Size = handler.Tell() - Start;
            return result;
        }

        protected abstract bool DoRead(FileHandler handler);

        protected abstract bool DoWrite(FileHandler handler);
    }
}
