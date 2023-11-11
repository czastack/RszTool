using System.Text;
using RszTool.Common;
using System.Runtime.InteropServices;
using System.Collections;

namespace RszTool
{
    public class FileHandler : IDisposable
    {
        public string? FilePath { get; }
        public Stream Stream { get; }
        public bool IsMemory { get; }
        public long Offset { get; set; }
        private StringTable? StringTable;
        private Sunday? searcher = new();

        public long Position => Stream.Position;

        public FileHandler() : this(new MemoryStream())
        {
        }

        public FileHandler(string path, bool isMemory = false)
        {
            FilePath = path;
            IsMemory = isMemory;
            FileStream fileStream = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            if (isMemory) {
                Stream = new MemoryStream();
                fileStream.CopyTo(Stream);
                fileStream.Dispose();
                Stream.Position = 0;
            } else {
                Stream = fileStream;
            }
        }

        public FileHandler(Stream stream)
        {
            IsMemory = stream is MemoryStream;
            Stream = stream;
        }

        ~FileHandler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream.Dispose();
            }
        }

        public FileHandler AsMemory()
        {
            var newStream = new MemoryStream();
            long pos = Stream.Position;
            Stream.Position = 0;
            Stream.CopyTo(newStream);
            Stream.Position = pos;
            newStream.Position = pos;
            return new FileHandler(newStream) { Offset = Offset };
        }

        public FileHandler WithOffset(long offset)
        {
            return new FileHandler(Stream) { Offset = offset };
        }

        public void Save(string? path = null)
        {
            if ((path == null || path == FilePath) && !IsMemory)
            {
                Stream.Flush();
            }
            else
            {
                path ??= FilePath;
                if (path == null) return;
                using FileStream fileStream = File.Create(path);
                long pos = Stream.Position;
                Stream.Position = 0;
                Stream.CopyTo(fileStream);
                Stream.Position = pos;
            }
        }

        public void Clear()
        {
            Stream.SetLength(0);
            Stream.Flush();
        }

        public long FileSize()
        {
            return Stream.Length;
        }

        public void Seek(long tell)
        {
            Stream.Position = tell + Offset;
        }

        public void Align(int alignment)
        {
            long delta = Stream.Position % alignment;
            if (delta != 0)
            {
                Stream.Position += alignment - delta;
            }
        }

        public void SeekOffsetAligned(int offset, int align = 4)
        {
            Seek(Utils.AlignSize(Tell() + offset, align));
        }

        public long Tell()
        {
            return Stream.Position - Offset;
        }

        public void Skip(long skip)
        {
            Stream.Seek(skip, SeekOrigin.Current);
        }

        public void CheckRange()
        {
            if (Stream.Position > Stream.Length)
            {
                throw new IndexOutOfRangeException($"Seek out of range {Stream.Position} > {Stream.Length}");
            }
        }

        public byte ReadByte()
        {
            return (byte)Stream.ReadByte();
        }

        public byte ReadByte(long tell)
        {
            Seek(tell);
            return (byte)Stream.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return (sbyte)Stream.ReadByte();
        }

        public sbyte ReadSByte(long tell)
        {
            Seek(tell);
            return (sbyte)Stream.ReadByte();
        }

        public char ReadChar()
        {
            return Read<char>();
        }

        public char ReadChar(long tell)
        {
            Seek(tell);
            return Read<char>();
        }

        public bool ReadBoolean()
        {
            return Stream.ReadByte() != 0;
        }

        public bool ReadBoolean(long tell)
        {
            Seek(tell);
            return Stream.ReadByte() != 0;
        }

        public short ReadShort()
        {
            return Read<short>();
        }

        public short ReadShort(long tell)
        {
            Seek(tell);
            return Read<short>();
        }

        public ushort ReadUShort()
        {
            return Read<ushort>();
        }

        public ushort ReadUShort(long tell)
        {
            Seek(tell);
            return Read<ushort>();
        }

        public int ReadInt(long tell)
        {
            Seek(tell);
            return Read<int>();
        }

        public int ReadInt()
        {
            return Read<int>();
        }

        public uint ReadUInt(long tell)
        {
            Seek(tell);
            return Read<uint>();
        }

        public uint ReadUInt()
        {
            return Read<uint>();
        }

        public long ReadInt64()
        {
            return Read<long>();
        }

        public long ReadInt64(long tell)
        {
            Seek(tell);
            return Read<long>();
        }

        public ulong ReadUInt64()
        {
            return Read<ulong>();
        }

        public ulong ReadUInt64(long tell)
        {
            Seek(tell);
            return Read<ulong>();
        }

        public float ReadFloat(long tell)
        {
            Seek(tell);
            return Read<float>();
        }

        public float ReadFloat()
        {
            return Read<float>();
        }

        public double ReadDouble(long tell)
        {
            Seek(tell);
            return Read<double>();
        }

        public double ReadDouble()
        {
            return Read<double>();
        }

        public int ReadBytes(byte[] buffer, int length = -1)
        {
            return Stream.Read(buffer, 0, length == -1 ? buffer.Length : length);
        }

        public int ReadBytes(byte[] buffer, long tell, int length = -1)
        {
            Seek(tell);
            return Stream.Read(buffer, 0, length == -1 ? buffer.Length : length);
        }

        public void WriteByte(byte value)
        {
            Stream.WriteByte(value);
        }

        public void WriteByte(long tell, byte value)
        {
            Seek(tell);
            Stream.WriteByte(value);
        }

        public void WriteSByte(sbyte value)
        {
            Stream.WriteByte((byte)value);
        }

        public void WriteSByte(long tell, sbyte value)
        {
            Seek(tell);
            Stream.WriteByte((byte)value);
        }

        public void WriteChar(char value)
        {
            Write(value);
        }

        public void WriteChar(long tell, char value)
        {
            Seek(tell);
            Write(value);
        }

        public void WriteBoolean(bool value)
        {
            Stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public void WriteBoolean(long tell, bool value)
        {
            Seek(tell);
            Stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public void WriteShort(short value)
        {
            Write(value);
        }

        public void WriteShort(long tell, short value)
        {
            Seek(tell);
            Write(value);
        }

        public void WriteUShort(ushort value)
        {
            Write(value);
        }

        public void WriteUShort(long tell, ushort value)
        {
            Seek(tell);
            Write(value);
        }

        public void WriteInt(int value)
        {
            Write(value);
        }

        public void WriteInt(long tell, int value)
        {
            Seek(tell);
            Write(value);
        }

        public void WriteUInt(uint value)
        {
            Write(value);
        }

        public void WriteUInt(long tell, uint value)
        {
            Seek(tell);
            Write(value);
        }

        public void WriteInt64(long value)
        {
            Write(value);
        }

        public void WriteInt64(long tell, long value)
        {
            Seek(tell);
            Write(value);
        }

        public void WriteUInt64(ulong value)
        {
            Write(value);
        }

        public void WriteUInt64(long tell, ulong value)
        {
            Seek(tell);
            Write(value);
        }

        public void WriteBytes(byte[] buffer, int length = -1)
        {
            Stream.Write(buffer, 0, length == -1 ? buffer.Length : length);
        }

        public void WriteBytes(long tell, byte[] buffer, int length = -1)
        {
            Seek(tell);
            WriteBytes(buffer, length);
        }

        public void FillBytes(byte value, int length)
        {
            if (length < 0)
            {
                // throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} must > 0");
                return;
            }
            if (length < 256)
            {
                Span<byte> bytes = stackalloc byte[length];
                bytes.Fill(value);
                WriteSpan<byte>(bytes);
            }
            else
            {
                Span<byte> bytes = stackalloc byte[256];
                bytes.Fill(value);
                int count = length >> 8;
                for (int i = 0; i < count; i++)
                {
                    WriteSpan<byte>(bytes);
                }
                if ((length & 0xFF) != 0)
                {
                    WriteSpan<byte>(bytes.Slice(0, length & 0xFF));
                }
            }
        }

        public void FillBytes(long tell, byte value, int length)
        {
            Seek(tell);
            FillBytes(value, length);
        }

        public static string MarshalStringTrim(string text)
        {
            int n = text.IndexOf('\0');
            if (n != -1)
            {
                text = text.Substring(0, n);
            }
            return text;
        }

        public string ReadWString(long pos = -1, int charCount = -1, bool jumpBack = true)
        {
            long originPos = Tell();
            if (pos != -1) Seek(pos);
            string? result = null;
            if (charCount > 1024)
            {
                throw new Exception($"{nameof(charCount)} {charCount} too large");
            }
            if (charCount == -1) charCount = 128;
            Span<char> buffer = charCount <= 128 ? stackalloc char[charCount] : new char[charCount];
            Span<byte> bytes = MemoryMarshal.AsBytes(buffer);
            int readCount = Stream.Read(bytes);
            if (readCount != 0)
            {
                int n = buffer.IndexOf((char)0);
                if (n != -1)
                {
                    result = n == 0 ? "" : new string(buffer.Slice(0, n));
                }
            }
            else
            {
                result = "";
            }
            if (result == null)
            {
                StringBuilder sb = new();
                sb.Append(buffer);
                do
                {
                    readCount = Stream.Read(bytes);
                    if (readCount != 0)
                    {
                        int n = buffer.IndexOf((char)0);
                        sb.Append(n != -1 ? buffer.Slice(0, n): buffer);
                        if (n != -1) break;
                    }
                } while (readCount == bytes.Length);
                result = sb.ToString();
            }
            if (jumpBack) Seek(originPos);
            return result ?? "";
        }

        public int ReadWStringLength(long pos = -1, int maxLen = -1, bool jumpBack = true)
        {
            long originPos = Tell();
            if (pos != -1) Seek(pos);
            int length = 0;
            char newByte = ReadChar();
            while (newByte != 0)
            {
                length++;
                newByte = ReadChar();
            }
            if (jumpBack) Seek(originPos);
            return length;
        }

        public bool WriteWString(string text)
        {
            return WriteSpan((ReadOnlySpan<char>)text) && Write<ushort>(0);
        }

        public T Read<T>() where T : struct
        {
            T value = default;
            Stream.Read(MemoryUtils.StructureAsBytes(ref value));
            return value;
        }

        public T Read<T>(long tell, bool jumpBack = true) where T : struct
        {
            long pos = Tell();
            Seek(tell);
            T value = Read<T>();
            if (jumpBack) Seek(pos);
            return value;
        }

        public int Read<T>(ref T value) where T : struct
        {
            return Stream.Read(MemoryUtils.StructureAsBytes(ref value));
        }

        public int Read<T>(long tell, ref T value, bool jumpBack = true) where T : struct
        {
            long pos = Tell();
            Seek(tell);
            int result = Read(ref value);
            if (jumpBack) Seek(pos);
            return result;
        }

        public bool Write<T>(T value) where T : struct
        {
            Stream.Write(MemoryUtils.StructureAsBytes(ref value));
            return true;
        }

        public bool Write<T>(long tell, T value, bool jumpBack = true) where T : struct
        {
            long pos = Tell();
            Seek(tell);
            bool result = Write(value);
            if (jumpBack) Seek(pos);
            return result;
        }

        public bool Write<T>(ref T value) where T : struct
        {
            Stream.Write(MemoryUtils.StructureAsBytes(ref value));
            return true;
        }

        public bool Write<T>(long tell, ref T value, bool jumpBack = true) where T : struct
        {
            long pos = Tell();
            Seek(tell);
            bool result = Write(ref value);
            if (jumpBack) Seek(pos);
            return result;
        }

        public byte[] ReadBytes(int length)
        {
            byte[] buffer = new byte[length];
            Stream.Read(buffer);
            return buffer;
        }

        public bool WriteBytes(byte[] buffer)
        {
            Stream.Write(buffer);
            return true;
        }

        /// <summary>读取数组</summary>
        public T[] ReadArray<T>(int length) where T : struct
        {
            T[] array = new T[length];
            Stream.Read(MemoryMarshal.AsBytes((Span<T>)array));
            return array;
        }

        /// <summary>读取数组</summary>
        public bool ReadArray<T>(T[] array) where T : struct
        {
            Stream.Read(MemoryMarshal.AsBytes((Span<T>)array));
            return true;
        }

        /// <summary>读取数组</summary>
        public bool ReadArray<T>(T[] array, int start = 0, int length = -1) where T : struct
        {
            if (length == -1 || length > array.Length - start)
            {
                length = array.Length - start;
            }
            Stream.Read(MemoryMarshal.AsBytes(array.AsSpan(start, length)));
            return true;
        }

        /// <summary>写入数组</summary>
        public bool WriteArray<T>(T[] array) where T : struct
        {
            Stream.Write(MemoryMarshal.AsBytes((ReadOnlySpan<T>)array));
            return true;
        }

        /// <summary>写入数组</summary>
        public bool WriteArray<T>(T[] array, int start = 0, int length = -1) where T : struct
        {
            if (length == -1 || length > array.Length - start)
            {
                length = array.Length - start;
            }
            Stream.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(array, start, length)));
            return true;
        }

        /// <summary>读取数组</summary>
        public bool ReadSpan<T>(Span<T> span) where T : struct
        {
            Stream.Write(MemoryMarshal.AsBytes(span));
            return true;
        }

        /// <summary>写入数组</summary>
        public bool WriteSpan<T>(ReadOnlySpan<T> span) where T : struct
        {
            Stream.Write(MemoryMarshal.AsBytes(span));
            return true;
        }

        /// <summary>查找字节数组</summary>
        public long FindBytes(byte[] pattern, in SearchParam param = default)
        {
            const int PAGE_SIZE = 8192;
            byte[] buffer = new byte[PAGE_SIZE];
            long addr = param.start;
            int sizeAligned = pattern.Length; // 数据大小对齐4字节

            if ((sizeAligned & 3) != 0)
            {
                sizeAligned += 4 - (sizeAligned & 3);
            }

            long end = param.end;
            if (end == -1)
            {
                end = FileSize();
            }

            searcher ??= new();
            searcher.Update(pattern, param.wildcard);

            while (addr < end)
            {
                int readCount = ReadBytes(buffer, addr, PAGE_SIZE);
                if (readCount != 0)
                {
                    int result = searcher.Search(buffer, 0, readCount, param.ordinal);
                    if (result != -1)
                    {
                        return addr + result;
                    }
                }
                addr += PAGE_SIZE - sizeAligned;
            }

            return -1;
        }

        public long FindFirst(byte[] pattern, in SearchParam param = default)
        {
            return FindBytes(pattern, param);
        }

        public long FindFirst(string pattern, in SearchParam param = default, Encoding? encoding = null)
        {
            encoding ??= Encoding.Unicode;
            return FindBytes(encoding.GetBytes(pattern), param);
        }

        public long FindFirst<T>(T pattern, in SearchParam param = default) where T : struct
        {
            return FindBytes(MemoryUtils.StructureRefToBytes(ref pattern), param);
        }

        public void InsertBytes(Span<byte> buffer, long position)
        {
            var stream = Stream;
            // 将当前位置保存到临时变量中
            long currentPosition = stream.Position;

            // 将流的位置设置为插入点
            stream.Position = position;

            // 将后续数据读取到临时缓冲区
            byte[] tempBuffer = new byte[stream.Length - position];
            int bytesRead = stream.Read(tempBuffer, 0, tempBuffer.Length);

            // 将插入数据写入到流中
            stream.Position = position;
            stream.Write(buffer);

            // 将后续数据写入到流中
            stream.Write(tempBuffer, 0, bytesRead);

            // 将流的位置恢复到原始位置
            stream.Position = currentPosition;
        }

        public void StringTableAdd(string? text)
        {
            if (text != null)
            {
                StringTable ??= new();
                StringTable.Add(text, Tell());
            }
        }

        /// <summary>
        /// 写入字符串表字符串和偏移
        /// </summary>
        public void StringTableFlush()
        {
            if (StringTable == null || StringTable.Count == 0) return;
            foreach (var item in StringTable)
            {
                item.TextStart = Tell();
                foreach (var offsetStart in item.OffsetStart)
                {
                    WriteInt64(offsetStart, item.TextStart);
                }
                Seek(item.TextStart);
                WriteWString(item.Text);
            }
            StringTable.Clear();
        }

        /// <summary>
        /// 写入字符串表的字符串
        /// </summary>
        public void StringTableWriteStrings()
        {
            if (StringTable == null || StringTable.Count == 0) return;
            foreach (var item in StringTable)
            {
                item.TextStart = Tell();
                WriteWString(item.Text);
            }
        }

        /// <summary>
        /// 写入字符串表的偏移，并清空数据
        /// </summary>
        public void StringTableFlushOffsets()
        {
            if (StringTable == null || StringTable.Count == 0) return;
            foreach (var item in StringTable)
            {
                if (item.TextStart == -1)
                {
                    throw new Exception($"StringStart of {item.Text} not set");
                }
                foreach (var offsetStart in item.OffsetStart)
                {
                    WriteInt64(offsetStart, item.TextStart);
                }
            }
            StringTable.Clear();
        }
    }


    public class StringTableItem
    {
        public string Text { get; }
        // 引用改字符串的偏移集合
        public HashSet<long> OffsetStart { get; } = new();
        public long TextStart { get; set; } = -1;

        public StringTableItem(string text)
        {
            Text = text;
        }
    }


    /// <summary>
    /// 待写入的字符串表
    /// </summary>
    public class StringTable : IEnumerable<StringTableItem>
    {
        private List<StringTableItem> Items { get; } = new();
        private Dictionary<string, StringTableItem> StringMap { get; } = new();

        public int Count => Items.Count;

        public void Clear()
        {
            Items.Clear();
            StringMap.Clear();
        }

        public void Add(string text, long offset)
        {
            if (!StringMap.TryGetValue(text, out var item))
            {
                StringMap[text] = item = new(text);
                Items.Add(item);
            }
            item.OffsetStart.Add(offset);
        }

        public IEnumerator<StringTableItem> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
    }
}
