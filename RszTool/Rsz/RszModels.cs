using System.Runtime.InteropServices;
using System.Text;

namespace RszTool.Rsz
{
    public class RSZMagic
    {
        public RSZMagic(RszFileHandler handler)
        {
            if (handler.realStart != -1)
            {
                handler.FSeek(handler.realStart);
                handler.realStart = -1;
            }

        }
    }

    public struct RSZHeader
    {
        public uint magic;
        public uint version;
        public int objectCount;
        public int instanceCount;
        public int userdataCount;
        private readonly int reserved;
        public long instanceOffset;
        public long dataOffset;
        public long userdataOffset;
    }

    public struct RTVersionRSZHeader
    {
        public uint magic;
        public uint version;
        public int objectCount;
        public int instanceCount;
        public long instanceOffset;
        public long dataOffset;
    }

    /* [ModelAutoOffsetField]
    public partial class RSZHeader : AdaptiveModel
    {
        OffsetField<uint> magic;
        OffsetField<uint> version;
        OffsetField<int> objectCount;
        OffsetField<int> instanceCount;
        OffsetField<int> userdataCount;
        // hidden
        OffsetField<int> reserved;
        OffsetField<long> instanceOffset;
        OffsetField<long> dataOffset;
        OffsetField<long> userdataOffset;

        public override bool Read(RszFileHandler handler, long start)
        {
            handler.FSeek(start);
            bool RTVersion = handler.RSZVersion != "RE7" || handler.RTVersion;
            StartRead(handler);
            ReadField(ref magic);
            ReadField(ref version);
            ReadField(ref objectCount);
            ReadField(ref instanceCount);
            if (RTVersion)
            {
                ReadField(ref userdataCount);
                ReadField(ref reserved);
            }
            ReadField(ref instanceOffset);
            ReadField(ref dataOffset);
            if (RTVersion)
            {
                ReadField(ref userdataOffset);
            }
            EndRead();
            return true;
        }
    } */

    struct ReadStruct
    {
        [MarshalAs(UnmanagedType.I4)]
        public int structType;

        [MarshalAs(UnmanagedType.U8)]
        public ulong addOffset;

        [MarshalAs(UnmanagedType.U8)]
        public ulong offset;
    }

    public static class ReadStructExtensions
    {
        /* public static void ReadReadStruct(this RszFileHandler handler, ref ReadStruct rs)
        {
            handler.FSeek((long)(rs.offset + rs.addOffset));

            switch (rs.structType)
            {
                case 0:
                    RSZMagic RSZ_0 = ReadRSZMagic();
                    break;

                case 1:
                    RSZInstance RSZ_1 = ReadRSZInstance();
                    break;

                default:
                    break;
            }

            if (rs.structType == 0 && handler.ReadUInt(startof(RSZ)) != 5919570)
            {
                Console.WriteLine($"RSZMagic not found at RSZ[{handler.getLevelRSZ(startof(RSZ))}] in BHVT header");
            }

            handler.FSeek(startof(rs.offset) + 8);
        } */
    }

    public struct FakeGameObject
    {
        public uint size0;
        [MarshalAs(UnmanagedType.ByValArray)]
        public string name;

        public uint size1;
        [MarshalAs(UnmanagedType.ByValArray)]
        public string tag;

        public uint timeScale;
    }


    public static class fakeGameObjectExtensions
    {
        public static void ReadFakeGameObject(this RszFileHandler handler, ref FakeGameObject obj)
        {
            handler.SeekOffsetAligned(0);
            obj.size0 = handler.ReadUInt();

            if (obj.size0 != 0 && handler.FTell() + obj.size0 * 2 <= handler.FileSize())
            {
                byte[] nameBytes = new byte[obj.size0 * 2];
                handler.ReadBytes(nameBytes, 0, nameBytes.Length);
                obj.name = Encoding.Unicode.GetString(nameBytes);
            }

            handler.SeekOffsetAligned(0);
            obj.size1 = handler.ReadUInt();

            if (obj.size1 != 0 && handler.FTell() + obj.size1 * 2 <= handler.FileSize())
            {
                byte[] tagBytes = new byte[obj.size1 * 2];
                handler.ReadBytes(tagBytes, 0, tagBytes.Length);
                obj.tag = Encoding.Unicode.GetString(tagBytes);
            }

            handler.SeekOffsetAligned(2);
            obj.timeScale = handler.ReadUInt();
        }
    }


    public struct BHVTCount
    {
        private byte listSize;
        public int Count;

        public BHVTCount(int listSize, RszFileHandler handler)
        {
            this.listSize = (byte)listSize;
            Count = handler.ReadInt();
        }

        public string ReadBHVTCount(BHVTCount c)
        {
            return c.Count.ToString();
        }

#if false
        public void WriteBHVTCount(ref BHVTCount c, string s, RszFileHandler handler)
        {
            int newCount = int.Parse(s);
            if (newCount - c.Count > 0)
            {
                int k, j, padding = 0;
                int addedSz = ((newCount - c.Count) * 4 * c.listSize);

                if (((newCount - c.Count) * 4 * c.listSize) % 16 != 0)
                {
                    padding = 0;
                    while ((handler.RSZOffset + addedSz + padding) % 16 != handler.RSZOffset % 16)
                        padding++;
                }

                FixBHVTOffsets(addedSz + padding, handler.RSZOffset);
                int extraStateBytes = 0;
                if (c.listSize == 6 && c.Count > 0) //states
                    extraStateBytes = ((startof(parentof(c)) + sizeof(parentof(c)) - (startof(c) + 4)) - (c.Count * 4 * c.listSize));

                for (k = c.listSize; k > 0; k--)
                {
                    handler.InsertBytes(startof(c) + 4 + ((c.Count * 4) * k) + (extraStateBytes), 4 * (newCount - c.Count), 0);
                    Console.WriteLine("inserting {0} bytes at {1} for +{2} new items", 4 * (newCount - c.Count), startof(c) + 4 + (c.Count * 4) * k, newCount - c.Count);
                }
                if (padding > 0)
                    handler.InsertBytes(RSZOffset + addedSz, padding, 0);
                handler.ShowRefreshMessage("");
            }
            c.Count = newCount;
        }
#endif
    }

    struct HashGenerator
    {
        [MarshalAs(UnmanagedType.U1)]
        byte dummy;

        [MarshalAs(UnmanagedType.LPStr)]
        string String_Form;

        [MarshalAs(UnmanagedType.I4)]
        int Hash_Form;

        [MarshalAs(UnmanagedType.U4)]
        uint Hash_Form_unsigned;

        public static string ReadStringToHash(ref HashGenerator h)
        {
            if (h.Hash_Form != 0)
            {
                string ss = string.Format("{0} ({1}) = {2}", h.Hash_Form, h.Hash_Form_unsigned, h.String_Form);
                return ss;
            }

            return "      [Input a String here to turn it into a Murmur3 Hash]";
        }

        public static void WriteStringToHash(ref HashGenerator h, string s)
        {
            h.String_Form = s;
            h.Hash_Form = (int)RszFileHandler.hash_wide(h.String_Form);
            h.Hash_Form_unsigned = RszFileHandler.hash_wide(h.String_Form);
        }

        public static string readRCOLWarning(ref uint u)
        {
            string s = u.ToString();

            // TODO
            // if (sizeof(RSZFile[0]) != u)
            //     SPrintf(s, "{0} -- Warning: Size does not match real size ({1})", s, sizeof(RSZFile[0]));

            return s;
        }
    }
}
