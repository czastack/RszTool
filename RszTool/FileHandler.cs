using int64 = System.Int64;
using System.Text;
using RszTool.Common;
using System.Runtime.InteropServices;

namespace RszTool
{
    public class FileHandler : IDisposable
    {
        public string? FilePath { get; }
        public Stream Stream { get; }
        public BinaryReader Reader { get; }
        public BinaryWriter Writer { get; }
        public bool IsMemory { get; }

        public List<StringToWrite> StringToWrites = new();

        private readonly Sunday searcher = new();

        public FileHandler(string path, bool isMemory = false)
        {
            FilePath = path;
            IsMemory = isMemory;
            FileStream fileStream = new(path, FileMode.Open);
            if (isMemory) {
                Stream = new MemoryStream();
                fileStream.CopyTo(Stream);
                fileStream.Dispose();
                Stream.Position = 0;
            } else {
                Stream = fileStream;
            }
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);
        }

        public FileHandler(Stream stream, bool isMemory = false)
        {
            IsMemory = isMemory;
            Stream = stream;
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);
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
                Reader.Dispose();
                Writer.Dispose();
                Stream.Dispose();
            }
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
                Stream.CopyTo(fileStream);
                Stream.Position = pos;
            }
        }

        public long FileSize()
        {
            return Stream.Length;
        }

        public void Seek(long tell)
        {
            Stream.Position = tell;
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

        public float ReadFloat(int64 tell)
        {
            Seek(tell);
            return Reader.ReadSingle();
        }

        public int ReadInt(int64 tell)
        {
            Seek(tell);
            return Reader.ReadInt32();
        }

        public int ReadInt()
        {
            return Reader.ReadInt32();
        }

        public uint ReadUInt(int64 tell)
        {
            Seek(tell);
            return Reader.ReadUInt32();
        }

        public uint ReadUInt()
        {
            return Reader.ReadUInt32();
        }

        public byte ReadUByte()
        {
            return Reader.ReadByte();
        }

        public byte ReadUByte(int64 tell)
        {
            Seek(tell);
            return Reader.ReadByte();
        }

        public sbyte ReadByte()
        {
            return Reader.ReadSByte();
        }

        public sbyte ReadByte(int64 tell)
        {
            Seek(tell);
            return Reader.ReadSByte();
        }

        public ushort ReadUShort()
        {
            return Reader.ReadUInt16();
        }

        public ushort ReadUShort(int64 tell)
        {
            Seek(tell);
            return Reader.ReadUInt16();
        }

        public long ReadInt64()
        {
            return Reader.ReadInt64();
        }

        public long ReadInt64(int64 tell)
        {
            Seek(tell);
            return Reader.ReadInt64();
        }

        public ulong ReadUInt64()
        {
            return Reader.ReadUInt64();
        }

        public ulong ReadUInt64(int64 tell)
        {
            Seek(tell);
            return Reader.ReadUInt64();
        }

        public int ReadBytes(byte[] buffer, int64 pos, int n)
        {
            Seek((uint)pos);
            return Stream.Read(buffer, 0, n);
        }

        public void WriteInt64(long value)
        {
            Writer.Write(value);
        }

        public void WriteInt64(int64 tell, long value)
        {
            Seek(tell);
            Writer.Write(value);
        }

        public void WriteUInt(uint value)
        {
            Writer.Write(value);
        }

        public void WriteUInt(int64 tell, uint value)
        {
            Seek(tell);
            Writer.Write(value);
        }

        public void WriteInt(int value)
        {
            Writer.Write(value);
        }

        public void WriteInt(int64 tell, int value)
        {
            Seek(tell);
            Writer.Write(value);
        }

        public void WriteUInt64(ulong value)
        {
            Writer.Write(value);
        }

        public void WriteUInt64(int64 tell, ulong value)
        {
            Seek(tell);
            Writer.Write(value);
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

        public string ReadWString(int64 pos = -1, int maxLen=-1, bool jumpBack = true)
        {
            long originPos = Tell();
            if (pos != -1) Seek(pos);
            string result = "";
            Span<byte> nullTerminator = stackalloc byte[] { (byte)0, (byte)0 };
            if (maxLen != -1)
            {
                byte[] buffer = new byte[maxLen * 2];
                int readCount = Stream.Read(buffer);
                if (readCount != 0)
                {
                    int n = ((ReadOnlySpan<byte>)buffer).IndexOf(nullTerminator);
                    result = Encoding.Unicode.GetString(buffer, 0, n != -1 ? n : readCount);
                }
            }
            else
            {
                StringBuilder sb = new();
                byte[] buffer = new byte[256];
                do
                {
                    int readCount = Stream.Read(buffer);
                    if (readCount != 0)
                    {
                        int n = ((ReadOnlySpan<byte>)buffer).IndexOf(nullTerminator);
                        sb.Append(Encoding.Unicode.GetString(buffer, 0, n != -1 ? n : readCount));
                        if (n != -1) break;
                    }
                    if (readCount != buffer.Length)
                    {
                        break;
                    }
                } while (true);
                result = sb.ToString();
            }
            if (jumpBack) Seek(originPos);
            return result;
        }

        public int ReadWStringLength(int64 pos = -1, int maxLen=-1, bool jumpBack = true)
        {
            long originPos = Tell();
            if (pos != -1) Seek(pos);
            int result = 0;
            Span<byte> nullTerminator = stackalloc byte[] { (byte)0, (byte)0 };
            if (maxLen != -1)
            {
                byte[] buffer = new byte[maxLen * 2];
                int readCount = Stream.Read(buffer);
                if (readCount != 0)
                {
                    int n = ((ReadOnlySpan<byte>)buffer).IndexOf(nullTerminator);
                    result = (n != -1 ? n : readCount) / 2;
                }
            }
            else
            {
                byte[] buffer = new byte[256];
                do
                {
                    int readCount = Stream.Read(buffer);
                    if (readCount != 0)
                    {
                        int n = ((ReadOnlySpan<byte>)buffer).IndexOf(nullTerminator);
                        result += (n != -1 ? n : readCount) / 2;
                        if (n != -1) break;
                    }
                    if (readCount != buffer.Length)
                    {
                        break;
                    }
                } while (true);
            }
            if (jumpBack) Seek(originPos);
            return result;
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

        public int Read<T>(ref T value) where T : struct
        {
            return Stream.Read(MemoryUtils.StructureAsBytes(ref value));
        }

        public bool Write<T>(T value) where T : struct
        {
            Stream.Write(MemoryUtils.StructureAsBytes(ref value));
            return true;
        }

        public bool Write<T>(ref T value) where T : struct
        {
            Stream.Write(MemoryUtils.StructureAsBytes(ref value));
            return true;
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
        public bool ReadArray<T>(T[] array, int start=0, int length=-1) where T : struct
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
        public bool WriteArray<T>(T[] array, int start=0, int length=-1) where T : struct
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

        public long Tell()
        {
            return Stream.Position;
        }

        public void Skip(long skip)
        {
            Stream.Seek(skip, SeekOrigin.Current);
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

        public void InsertBytes(Span<byte> buffer, int64 position)
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

        public void AddStringToWrite(string? text)
        {
            if (text != null)
            {
                StringToWrites.Add(new(Tell(), text));
            }
        }

        public void FlushStringToWrite()
        {
            foreach (var item in StringToWrites)
            {
                long pos = Tell();
                WriteInt64(item.OffsetStart, pos);
                Seek(pos);
                WriteWString(item.Text);
            }
            StringToWrites.Clear();
        }
    }
}
