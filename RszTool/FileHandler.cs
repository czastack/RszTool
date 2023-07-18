using int64 = System.Int64;
using System.Text;
using RszTool.Common;

namespace RszTool
{
    public class FileHandler : IDisposable
    {
        public FileStream FileStream { get; private set; }
        public BinaryReader Reader { get; private set; }
        public BinaryWriter Writer { get; private set; }

        private Sunday searcher = new Sunday();

        public FileHandler(string path)
        {
            FileStream = new FileStream(path, FileMode.Open);
            Reader = new BinaryReader(FileStream);
            Writer = new BinaryWriter(FileStream);
        }

        public void Dispose()
        {
            Reader.Dispose();
            Writer.Dispose();
            FileStream.Dispose();
        }

        public long FileSize()
        {
            return FileStream.Length;
        }

        public void FSeek(long tell)
        {
            FileStream.Position = tell;
        }

        void align(uint alignment)
        {
            long delta = FileStream.Position % alignment;
            if (delta != 0)
            {
                FileStream.Position += alignment - delta;
            }
        }

        public float ReadFloat(int64 tell)
        {
            FSeek(tell);
            return Reader.ReadSingle();
        }

        public int ReadInt(int64 tell)
        {
            FSeek(tell);
            return Reader.ReadInt32();
        }

        public int ReadInt()
        {
            return Reader.ReadInt32();
        }

        public uint ReadUInt(int64 tell)
        {
            FSeek(tell);
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
            FSeek(tell);
            return Reader.ReadByte();
        }

        public sbyte ReadByte()
        {
            return Reader.ReadSByte();
        }

        public sbyte ReadByte(int64 tell)
        {
            FSeek(tell);
            return Reader.ReadSByte();
        }

        public ushort ReadUShort()
        {
            return Reader.ReadUInt16();
        }

        public ushort ReadUShort(int64 tell)
        {
            FSeek(tell);
            return Reader.ReadUInt16();
        }

        public long ReadInt64()
        {
            return Reader.ReadInt64();
        }

        public long ReadInt64(int64 tell)
        {
            FSeek(tell);
            return Reader.ReadInt64();
        }

        public ulong ReadUInt64()
        {
            return Reader.ReadUInt64();
        }

        public ulong ReadUInt64(int64 tell)
        {
            FSeek(tell);
            return Reader.ReadUInt64();
        }

        public int ReadBytes(byte[] buffer, int64 pos, int n)
        {
            FSeek((uint)pos);
            return FileStream.Read(buffer, 0, n);
        }

        public void WriteInt64(long value)
        {
            Writer.Write(value);
        }

        public void WriteInt64(int64 tell, long value)
        {
            FSeek(tell);
            Writer.Write(value);
        }

        public void WriteUInt(uint value)
        {
            Writer.Write(value);
        }

        public void WriteUInt(int64 tell, uint value)
        {
            FSeek(tell);
            Writer.Write(value);
        }

        public void WriteInt(int value)
        {
            Writer.Write(value);
        }

        public void WriteInt(int64 tell, int value)
        {
            FSeek(tell);
            Writer.Write(value);
        }

        public void WriteUInt64(ulong value)
        {
            Writer.Write(value);
        }

        public void WriteUInt64(int64 tell, ulong value)
        {
            FSeek(tell);
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

        public string ReadWString(int64 pos, int maxLen=-1)
        {
            FSeek(pos);
            string result = "";
            Span<byte> nullTerminator = stackalloc byte[] { (byte)0, (byte)0 };
            if (maxLen != -1)
            {
                byte[] buffer = new byte[maxLen * 2];
                int readCount = FileStream.Read(buffer);
                if (readCount != 0)
                {
                    int n = ((ReadOnlySpan<byte>)buffer).IndexOf(nullTerminator);
                    result = System.Text.Encoding.Unicode.GetString(buffer, 0, n != -1 ? n : readCount);
                }
            }
            else
            {
                StringBuilder sb = new();
                byte[] buffer = new byte[256];
                do
                {
                    int readCount = FileStream.Read(buffer);
                    if (readCount != 0)
                    {
                        int n = ((ReadOnlySpan<byte>)buffer).IndexOf(nullTerminator);
                        sb.Append(System.Text.Encoding.Unicode.GetString(buffer, 0, n != -1 ? n : readCount));
                        if (n != -1) break;
                    }
                    if (readCount != buffer.Length)
                    {
                        break;
                    }
                } while (true);
                result = sb.ToString();
            }
            return result;
        }

        public int ReadWStringLength(int64 pos, int maxLen=-1)
        {
            FSeek(pos);
            int result = 0;
            Span<byte> nullTerminator = stackalloc byte[] { (byte)0, (byte)0 };
            if (maxLen != -1)
            {
                byte[] buffer = new byte[maxLen * 2];
                int readCount = FileStream.Read(buffer);
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
                    int readCount = FileStream.Read(buffer);
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
            return result;
        }

        public long FTell()
        {
            return FileStream.Position;
        }

        public void FSkip(long skip)
        {
            FileStream.Seek(skip, SeekOrigin.Current);
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
    }
}
