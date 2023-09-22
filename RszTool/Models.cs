using System.Reflection;
using System.Runtime.CompilerServices;
using RszTool.Common;

namespace RszTool
{
    public class DynamicModel
    {
        public long Start { get; private set; }
        protected static Stack<FileHandler> FileHandlers = new();

        protected void StartRead(FileHandler handler)
        {
            Start = handler.FTell();
            FileHandlers.Append(handler);
        }

        protected void EndRead()
        {
            FileHandlers.Pop();
        }

        protected bool ReadField<T>(ref DynamicField<T> field) where T : struct
        {
            return ReadField(FileHandlers.Peek(), ref field);
        }

        protected bool ReadField<T>(FileHandler handler, ref DynamicField<T> field) where T : struct
        {
            field.Offset = (int)(handler.FTell() - Start);
            field.Value = handler.Read<T>();
            return true;
        }

        protected bool WriteField<T>(FileHandler handler, in DynamicField<T> field) where T : struct
        {
            field.Write(handler);
            return true;
        }

        public bool Write(FileHandler handler)
        {
            long start = handler.FTell();
            foreach (var fieldInfo in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var value = fieldInfo.GetValue(this);
                if (value is IDynamicField field)
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


    public interface IDynamicField
    {
        int Offset { get; }
        bool Write(FileHandler handler);
    }


    public struct DynamicField<T> where T : struct
    {
        public T Value { get; set; }
        public int Offset { get; set; }

        public static implicit operator T(DynamicField<T> field) => field.Value;

        public bool Write(FileHandler handler)
        {
            return handler.Write(Value);
        }
    }
}
