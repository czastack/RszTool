using System.Text.RegularExpressions;
using uint64 = System.UInt64;
using int64 = System.Int64;
using System.Text;

namespace RszTool
{
    public class RszFileParser
    {
        // Game Extracted Path
        const string DMC5Path = "F:\\modmanager\\REtool\\DMC_chunk_000\\natives\\x64\\";
        const string RE2Path = "F:\\modmanager\\REtool\\RE2_chunk_000\\natives\\x64\\";
        const string RE3Path = "F:\\modmanager\\REtool\\RE3_chunk_000\\natives\\stm\\";
        const string RE4Path = "H:\\modding\\REtool\\RE4demo_chunk_000\\natives\\stm\\";
        const string RE7Path = "F:\\modmanager\\REtool\\RE7_chunk_000\\natives\\x64\\";
        const string RE8Path = "F:\\modmanager\\REtool\\RE8_chunk_000\\natives\\stm\\";
        const string MHRPath = "F:\\modmanager\\REtool\\MHR_chunk_000\\natives\\stm\\";
        const string RE2RTPath = "F:\\modmanager\\REtool\\RE2RT_chunk_000\\natives\\stm\\";
        const string RE3RTPath = "F:\\modmanager\\REtool\\RE3RT_chunk_000\\natives\\stm\\";
        const string RE7RTPath = "F:\\modmanager\\REtool\\RE7RT_chunk_000\\natives\\stm\\";
        const string SF6Path = "F:\\modmanager\\REtool\\SF6Beta_chunk_000\\natives\\stm\\";
        const string GTrickPath = "F:\\modmanager\\REtool\\GT_chunk_000\\natives\\stm\\";

        string RSZVersion        = "RE4"; // change between RE2, RE3, RE8, DMC5 or MHRise
        bool   RTVersion         = true;  // Use Ray-Tracing Update file formats for RE7, RE2R and RE3R (subject to AutoDetectGame)
        bool   Nesting           = true;  // Attempt to nest class instances inside eachother
        bool   ShowAlignment     = false; // Show metadata for each variable
        bool   ShowChildRSZs     = false; // Show all RSZs one after another, non-nested. Disabling hides nested RSZHeaders
        bool   UseSpacers        = true;  // Show blank rows between some structs
        bool   AutoDetectGame    = true;  // Automatically detect RSZVersion based on the name + ext of the file being viewed
        bool   RedetectBHVT      = true;  // Will automatically redetect the next BHVT node if there is a problem
        bool   HideRawData       = false; // Hides RawData struct
        bool   HideRawNodes      = true;  // Hides RawNodes struct
        bool   SortRequestSets   = true;  // Sorts RCOL RequestSets by their IDs
        bool   ExposeUserDatas   = true;  // Makes RSZFiles that contain embedded userDatas start after the Userdatas, for ctrl+J jump
        bool   SeekByGameObject  = false; // Will automatically seek between detected GameObjects to fix reading errors
        bool   ReadNodeFullNames = true;  // Reads FSM Node names with the names of all their parents (may be taxing)

        private FileStream fileStream;
        private BinaryReader reader;
        private string filename;
        private string Local_Directory;
        private string JsonPath;
        private string extractedDir;
        private string xFmt;

        int RSZOffset;
        int BHVTStart;
        int UVARStart;
        int lastVarEnd;
        int realStart = -1;
        int level;
        int finished;
        int broken;
        byte silenceMessages;
        byte isAIFile;
        byte[] magic = new byte[4];
        ushort headerStringsCount;
        uint[] headerStrings;
        uint[] dummyArr = new uint[1];
        byte[] PasteBuffer = new byte[100000]; // 100KB buffer

        public void Init()
        {
            // variables:
            int i, j, k, m, n, o, h, temp;
            int matchSize, lastGameObject;
            int[] uniqueHashes = new int[5000];
            int hashesLen;
            int noRetry;

            RSZOffset = FindFirst("RSZ", 1, 0, 0, 0.0, 1, 0, 0, 24);
            BHVTStart = FindFirst("BHVT", 1, 0, 0, 0.0, 1, 0, 0, 24);
            UVARStart = (int)BHVTStart;
            headerStrings = new uint[1 + RSZOffset / 64];

            if (detectedHash(4) && !detectedHash(0))
                ReadBytes(magic, 4, 4);
            else
                ReadBytes(magic, 0, 4);

            if (ShowAlignment)
            {
                int varLen = 0;
                long maxVars = ((FileSize() - RSZOffset) / 6);
                if (maxVars > 1000000)
                    maxVars = 1000000;
                uint[] offs = new uint[maxVars];
                uint[] aligns = new uint[maxVars];
                uint[] sizes = new uint[maxVars];
            }
            else
            {
                int varLen = 0;
            }

            filename = GetFileName();
            extractedDir = DMC5Path;
            Local_Directory = Path.GetDirectoryName(filename)!;
            uint findValue = (uint)Local_Directory.IndexOf("natives");
            Local_Directory = Local_Directory.Remove((int)findValue, Local_Directory.Length - (int)findValue) + "natives\\";
            string dir = Local_Directory.ToLower();

            ushort maxLevels = 0;
            ulong[] RSZAddresses = new ulong[6000];
            h = FindFirst(5919570, 1, 0, 0, 0.0, 1, 0, 0, 24) + 4;
            while (h != 3)
            {
                RSZAddresses[maxLevels] = h - 4;
                maxLevels++;
                h = FindFirst(5919570, 1, 0, 0, 0.0, 1, h, 0, 24) + 4;
            }
            int RSZFileMaxDivs = maxLevels / 100 + 1;
            uint[] RSZFileWaypoints = new uint[RSZFileMaxDivs];
            uint RSZFileDivCounter;

            if (AutoDetectGame) {
                if (RTVersion && (Regex.IsMatch(filename, "scn.1[89]") || filename.Contains("pfb.16") || filename.Contains("motfsm2.30")
                    || filename.Contains("rcol.10") || filename.Contains("fsmv2.30") || filename.Contains("rcol.2")))
                {
                    Console.WriteLine("Detected Pre-RayTracing file extension");
                    RTVersion = false;
                }

                o = 0;
                string xFmt = "x64\\";

                if (dir.Contains("dmc5") || dir.Contains("evil may") || dir.Contains("dmc_") || dir.Contains("dmc "))
                {
                    RSZVersion = "DMC5";
                    o = 1;
                    extractedDir = DMC5Path;
                    Console.WriteLine("Detected DMC in filepath");
                }
                else if (dir.Contains("sf6") || dir.Contains("reet fighter"))
                {
                    RSZVersion = "SF6";
                    o = 1;
                    extractedDir = SF6Path;
                    xFmt = "stm\\";
                    Console.WriteLine("Detected SF6 in filepath");
                }
                else if (dir.Contains("re2") || dir.Contains("evil 2"))
                {
                    RSZVersion = "RE2";
                    o = 1;
                    if (RTVersion)
                    {
                        extractedDir = RE2RTPath.ToLower();
                        xFmt = "stm\\";
                    }
                    else
                    {
                        extractedDir = RE2Path.ToLower();
                        xFmt = "x64\\";
                    }
                    Console.WriteLine("Detected RE2 in filepath");
                }
                else if (dir.Contains("re3") || dir.Contains("evil 3"))
                {
                    RSZVersion = "RE3";
                    o = 1;
                    if (RTVersion)
                        extractedDir = RE3RTPath.ToLower();
                    else
                        extractedDir = RE3Path.ToLower();
                    xFmt = "stm\\";
                    Console.WriteLine("Detected RE3 in filepath");
                }
                else if (dir.Contains("re4") || dir.Contains("evil 4"))
                {
                    RSZVersion = "RE4";
                    o = 1;
                    extractedDir = RE4Path.ToLower();
                    xFmt = "stm\\";
                    Console.WriteLine("Detected RE4 in filepath");
                }
                else if (dir.Contains("re8") || dir.Contains("evil 8") || dir.Contains("illage"))
                {
                    RSZVersion = "RE8";
                    o = 1;
                    extractedDir = RE8Path.ToLower();
                    xFmt = "stm\\";
                    Console.WriteLine("Detected RE8 in filepath");
                }
                else if (dir.Contains("re7") || dir.Contains("evil 7"))
                {
                    RSZVersion = "RE7";
                    o = 1;
                    if (RTVersion)
                    {
                        extractedDir = RE7RTPath.ToLower();
                        xFmt = "stm\\";
                    }
                    else
                    {
                        extractedDir = RE7Path.ToLower();
                        xFmt = "x64\\";
                    }
                    Console.WriteLine("Detected RE7 in filepath");
                }
                else if (dir.Contains("\\mhr") || dir.Contains("nter R") || dir.Contains("Rise"))
                {
                    RSZVersion = "MHRise";
                    o = 1;
                    extractedDir = MHRPath.ToLower();
                    xFmt = "stm\\";
                    Console.WriteLine("Detected MHRise in filepath");
                }
                else if (dir.Contains("\\gt") || dir.Contains("host tr") || dir.Contains("trick"))
                {
                    RSZVersion = "GTrick";
                    o = 1;
                    extractedDir = GTrickPath.ToLower();
                    xFmt = "stm\\";
                    Console.WriteLine("Detected Ghost Trick in filepath");
                }

                Local_Directory += xFmt;
            }

            Local_Directory = Local_Directory.ToLower();
            string JsonPath = (Path.GetDirectoryName(filename) + "rsz" + RSZVersion).ToLower();
            if (RTVersion && (RSZVersion == "RE2" || RSZVersion == "RE7" || RSZVersion == "RE3"))
                JsonPath += "rt";
            if (RSZVersion == "SF6" && dir.Contains("beta"))
                JsonPath += "beta";

            JsonPath += ".json";
            ParseJson(JsonPath);

            if (RSZOffset > -1 && AutoDetectGame && !(RSZVersion == "RE7" && !RTVersion))
                AutoDetectVersion();

            RTVersion = (RSZVersion == "RE2" || RSZVersion == "RE7" || RSZVersion == "RE3") ? RTVersion : false;

            string GameVersion = RSZVersion;
            bool IsRayTracing = RTVersion;
        }

        void CheckHashesForRT()
        {
            int firstGameObj = FindFirst(3372393495, 1, 0, 0, 0.0, 1, 0, 0, 24); //via.GameObject
            int firstFolder = FindFirst(2929908172, 1, 0, 0, 0.0, 1, 0, 0, 24);  //via.Folder
            int firstFSM = FindFirst(4193703126, 1, 0, 0, 0.0, 1, 0, 0, 24);     //via.motion.Fsm2ActionPlayMotion
            int firstRCOL = FindFirst(4150774079, 1, 0, 0, 0.0, 1, 0, 0, 24);     //via.physics.UserData

            if (firstGameObj != -1)
            {
                RTVersion = (ReadUInt(firstGameObj + 4) == 216572408); //check if CRC version is new
            }
            else if (firstFolder != -1)
            {
                RTVersion = (ReadUInt(firstFolder + 4) == 2121287109);
            }
            else if (firstFSM != -1)
            {
                RTVersion = (ReadUInt(firstFSM + 4) == 1025596507 && Regex.IsMatch(filename, "motfsm2\\.36$") == false);
            }
            else if (firstRCOL != -1)
            {
                RTVersion = ((ReadUInt(firstRCOL + 4) == 374943849) && Regex.IsMatch(filename, "rcol\\.11$") == false);
            }
        }

        void AutoDetectVersion() {
            string hashName;
            uint checkedVersions, instanceCount = 0, objectCount, hash, zz, varsChecked = 0;
            bool origRTVersion = RTVersion;
            int badCRCs;
            string origVersion = RSZVersion, origExtractedDir = (string)extractedDir, origXFmt = xFmt,
                origLocal_Directory = Local_Directory, origJsonPath = JsonPath;

            FSeek(RSZOffset);
            if (FTell() + 12 < FileSize())
            {
                instanceCount = ReadUInt(FTell() + 12);
                objectCount = ReadUInt(FTell() + 8);
            }
            if (instanceCount != 0) {
                FSeek(ReadUInt(RSZOffset+24) + RSZOffset + 8);
                //if (ReadUInt64() != 0) {
                //    if (RSZVersion != "RE7")
                //        Printf("RSZVersion auto detected to RE7\n");
                //    RSZVersion = "RE7"; extractedDir = RE7Path; xFmt = "x64\\";
                //    return;
                //}
                //if (RSZVersion == "RE3")

                for (zz=1; zz<instanceCount; zz++) {
                    if (varsChecked > 100) break;
                    hash = ReadUInt();
                    hashName = ReadHashName(hash);
                    checkedVersions = 0;
                    if (hash != 0 && (hashName == "Unknown Class!") && hashName != "via.physics.UserData" && hashName != "via.physics.RequestSetColliderUserData") {
                        //Printf("%s %i %i\n", hashName, zz, FTell());
                        while (checkedVersions <= 6 && (hashName == "Unknown Class!")) { //|| (RSZVersion == "RE3" && (hashName == "via.physics.UserData" || hashName == "via.physics.RequestSetColliderUserData"))
                            switch (checkedVersions) {
                                case 0: RSZVersion = "DMC5"; extractedDir = DMC5Path; xFmt = "x64\\"; break;
                                case 1: RSZVersion = "RE2"; extractedDir = RTVersion ? RE2RTPath : RE2Path; xFmt = RTVersion ? "stm\\" : "x64\\";  break;
                                case 2: RSZVersion = "RE3"; extractedDir = RTVersion ? RE3RTPath : RE3Path; xFmt = "stm\\";  break;
                                case 3: RSZVersion = "RE8"; extractedDir = RE8Path; xFmt = "stm\\"; break;
                                case 4: RSZVersion = "MHRise"; extractedDir = MHRPath; xFmt = "stm\\"; break;
                                case 5: RSZVersion = "RE7"; extractedDir = RTVersion ? RE7RTPath : RE7Path; xFmt = RTVersion ? "stm\\" : "x64\\";  break;
                                case 6: RSZVersion = "SF6"; extractedDir = SF6Path; xFmt = "stm\\"; break;
                                default: break;
                            }

                            JsonPath = Lower(Path.GetDirectoryName(filename) + "rsz" + RSZVersion);
                            if (RTVersion && (RSZVersion == "RE2" || RSZVersion == "RE3" || RSZVersion == "RE7"))
                                JsonPath = JsonPath + "rt";
                            JsonPath = JsonPath + ".json";
                            Local_Directory = dir + xFmt;
                            ParseJson(JsonPath);
                            hashName = ReadHashName(hash);
                            checkedVersions++;
                        }
                        if (hashName == "Unknown Class!") { //checkedVersions == 8 &&
                            RSZVersion = origVersion; extractedDir = origExtractedDir; xFmt = origXFmt;
                            Local_Directory = origLocal_Directory; JsonPath = origJsonPath; RTVersion = origRTVersion;
                        } else {
                            Printf("RSZVersion auto detected to %s\n", RSZVersion);
                            break;
                        }
                    } //else
                    //  Printf("%s\n", hashName);
                    varsChecked++;
                    FSkip(8);
                    if (varsChecked > 15)
                        break;
                }
            }
            FSeek(0);
        }

        void align(uint alignment)
        {
            long delta = fileStream.Position % alignment;
            if (delta != 0)
            {
                fileStream.Position += alignment - delta;
            }
        }

        private long FileSize()
        {
            return fileStream.Length;
        }

        private void FSeek(int64 tell)
        {
            fileStream.Position = tell;
        }

        private float ReadFloat(int64 tell)
        {
            FSeek(tell);
            return reader.ReadSingle();
        }

        private int ReadInt(int64 tell)
        {
            FSeek(tell);
            return reader.ReadInt32();
        }

        private int ReadInt()
        {
            return reader.ReadInt32();
        }

        private uint ReadUInt(int64 tell)
        {
            FSeek(tell);
            return reader.ReadUInt32();
        }

        private uint ReadUInt()
        {
            return reader.ReadUInt32();
        }

        private byte ReadUByte()
        {
            return reader.ReadByte();
        }

        private byte ReadUByte(int64 tell)
        {
            FSeek(tell);
            return reader.ReadByte();
        }

        private sbyte ReadByte()
        {
            return reader.ReadSByte();
        }

        private sbyte ReadByte(int64 tell)
        {
            FSeek(tell);
            return reader.ReadSByte();
        }

        private ushort ReadUShort()
        {
            return reader.ReadUInt16();
        }

        private ushort ReadUShort(int64 tell)
        {
            FSeek(tell);
            return reader.ReadUInt16();
        }

        private void ReadBytes(byte[] buffer, int64 pos, int n)
        {
            FSeek((uint)pos);
            fileStream.Read(buffer, 0, n);
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

        private string ReadWString(int64 pos, int maxLen=-1)
        {
            FSeek(pos);
            string result = "";
            Span<byte> nullTerminator = stackalloc byte[] { (byte)0, (byte)0 };
            if (maxLen != -1)
            {
                byte[] buffer = new byte[maxLen * 2];
                int readCount = fileStream.Read(buffer);
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
                    int readCount = fileStream.Read(buffer);
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

        private int ReadWStringLength(int64 pos, int maxLen=-1)
        {
            FSeek(pos);
            int result = 0;
            Span<byte> nullTerminator = stackalloc byte[] { (byte)0, (byte)0 };
            if (maxLen != -1)
            {
                byte[] buffer = new byte[maxLen * 2];
                int readCount = fileStream.Read(buffer);
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
                    int readCount = fileStream.Read(buffer);
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

        private long FTell()
        {
            return fileStream.Position;
        }

        private void FSkip(long skip)
        {
            fileStream.Seek(skip, SeekOrigin.Current);
        }

        bool detectedColorVector(int64 tell)
        {
            if (tell + 16 <= FileSize())
            {
                float R = ReadFloat(tell), G = ReadFloat(tell + 4), B = ReadFloat(tell + 8), A = ReadFloat(tell + 12);
                return ((R >= 0 && G >= 0 && B >= 0 && (A == 0 || A == 1)) && ((R + G + B + A <= 4) || (R + G + B + A) % 1.0 == 0));
            }
            return false;
        }

        float readColorFloat(int64 tell)
        {
            float colorFlt = ReadFloat(tell);
            if (colorFlt <= 1)
            {
                colorFlt = (uint)(colorFlt * 255.0f + 0.5);
                if (colorFlt > 255)
                    return 255;
            }
            return colorFlt;
        }

        bool detectedFloat(int64 offset)
        {
            if (offset + 4 <= FileSize())
            {
                float flt = ReadFloat(offset);
                if (BHVTStart != -1)
                    return (ReadUByte(offset + 3) < 255 && (Math.Abs(flt) > 0.000001 && Math.Abs(flt) < 100000) || ReadInt(offset) == 0);
                else return (ReadUByte(offset + 3) < 255 && (Math.Abs(flt) > 0.0000001 && Math.Abs(flt) < 10000000) || ReadInt(offset) == 0);
            }
            return false;
        }

        bool detectedStringSm(int64 offset)
        {
            if (offset + 4 <= FileSize() && ReadUShort(offset - 2) == 0 &&
                (ReadByte(offset) != 0 || ReadUShort(offset) == 0) &&
                (ReadByte(offset + 1) == 0 || ReadWStringLength(offset) > 5))
                //if (sizeof(ReadWString(offset)) >= 2)
                return true;
            return false;
        }

        bool detectedString(int64 offset)
        {
            if (offset + 6 <= FileSize() && ReadByte(offset) != 0 && ReadByte(offset + 1) == 0 &&
                ReadByte(offset + 2) != 0 && ReadByte(offset + 3) == 0 && ReadByte(offset + 4) != 0)
                return true;
            return false;
        }

        bool detectedNode(int64 tell)
        {
            if (tell + 12 < FileSize() && ReadInt(tell - 4) == 0 && ReadInt(tell) != -1 &&
                detectedHash(tell) && ReadInt(tell + 8) != 0 &&
                detectedStringSm(startof(Header.BHVT.mNamePool) + 4 + (ReadUInt(tell + 8) * 2)))
                return true;
            return false;
        }

        bool detectedBools(int64 tell) {
            uint nonBoolTotal = 0;
            for (int o=0; o<4; o++)
                if (ReadUByte(tell + o) > 1)
                    nonBoolTotal++;
            if (nonBoolTotal == 0)
                return true;
            return false;
        }

        bool detectedHash(int64 tell) {
            int tst = ReadInt(tell);
            if (tst == -1 || tst == 0)
                return false;
            int nonHashTotal = 0;
            for (int o=0; o<4; o++)
                if (ReadUByte(tell + o) == 0)
                    nonHashTotal++;
            if (nonHashTotal <= 1)
                return true;
            return false;
        }

        ulong getAlignedOffset(ulong tell, uint alignment) {
            ulong offset = tell;
            switch (alignment) {
                case 2:  offset = tell + (tell % 2); break;  // 2-byte
                case 4:  offset = (tell + 3) & 0xFFFFFFFFFFFFFFFC; break;  // 4-byte
                case 8:  offset = (tell + 7) & 0xFFFFFFFFFFFFFFF8; break;  // 8-byte
                case 16: offset = (tell + 15) & 0xFFFFFFFFFFFFFFF0; break; // 16-byte
                default: break;
            }
            return offset;
        }
    }

    enum BHVTlvl {
        id_All = -1,
        id_Actions = 0,
        id_Selectors = 1,
        id_SelectorCallers = 2,
        id_Conditions = 3,
        id_TransitionEvents = 4,
        id_ExpressionTreeConditions = 5,
        id_StaticActions = 6,
        id_StaticSelectorCallers = 7,
        id_StaticConditions = 8,
        id_StaticTransitionEvents = 9,
        id_StaticExpressionTreeConditions = 10,
        id_Transition = 11,
        id_Paths = 12,
        id_Tags = 13,
        id_NameHash = 14
    }

    enum TypeIDs : uint {
        ukn_error = 0,
        ukn_type,
        not_init,
        class_not_found,
        out_of_range,
        Undefined_tid,
        Object_tid,
        Action_tid,
        Struct_tid,
        NativeObject_tid,
        Resource_tid,
        UserData_tid,
        Bool_tid,
        C8_tid,
        C16_tid,
        S8_tid,
        U8_tid,
        S16_tid,
        U16_tid,
        S32_tid,
        U32_tid,
        S64_tid,
        U64_tid,
        F32_tid,
        F64_tid,
        String_tid,
        MBString_tid,
        Enum_tid,
        Uint2_tid,
        Uint3_tid,
        Uint4_tid,
        Int2_tid,
        Int3_tid,
        Int4_tid,
        Float2_tid,
        Float3_tid,
        Float4_tid,
        Float3x3_tid,
        Float3x4_tid,
        Float4x3_tid,
        Float4x4_tid,
        Half2_tid,
        Half4_tid,
        Mat3_tid,
        Mat4_tid,
        Vec2_tid,
        Vec3_tid,
        Vec4_tid,
        VecU4_tid,
        Quaternion_tid,
        Guid_tid,
        Color_tid,
        DateTime_tid,
        AABB_tid,
        Capsule_tid,
        TaperedCapsule_tid,
        Cone_tid,
        Line_tid,
        LineSegment_tid,
        OBB_tid,
        Plane_tid,
        PlaneXZ_tid,
        Point_tid,
        Range_tid,
        RangeI_tid,
        Ray_tid,
        RayY_tid,
        Segment_tid,
        Size_tid,
        Sphere_tid,
        Triangle_tid,
        Cylinder_tid,
        Ellipsoid_tid,
        Area_tid,
        Torus_tid,
        Rect_tid,
        Rect3D_tid,
        Frustum_tid,
        KeyFrame_tid,
        Uri_tid,
        GameObjectRef_tid,
        RuntimeType_tid,
        Sfix_tid,
        Sfix2_tid,
        Sfix3_tid,
        Sfix4_tid,
        Position_tid,
        F16_tid,
        End_tid,
        Data_tid
    };

    public struct BHVTCount
    {
        private byte listSize;
        public int Count;

        public BHVTCount(int listSize)
        {
            this.listSize = (byte)listSize;
            Count = 0;
        }

        public string ReadBHVTCount(BHVTCount c)
        {
            return c.Count.ToString();
        }

        public void WriteBHVTCount(ref BHVTCount c, string s)
        {
            int newCount = int.Parse(s);
            if (newCount - c.Count > 0)
            {
                int k, j, padding;
                int addedSz = ((newCount - c.Count) * 4 * c.listSize);

                if (((newCount - c.Count) * 4 * c.listSize) % 16 != 0)
                {
                    padding = 0;
                    while ((RSZOffset + addedSz + padding) % 16 != RSZOffset % 16)
                        padding++;
                }

                FixBHVTOffsets(addedSz + padding, RSZOffset);
                int extraStateBytes = 0;
                if (c.listSize == 6 && c.Count > 0) //states
                    extraStateBytes = ((startof(parentof(c)) + sizeof(parentof(c)) - (startof(c) + 4)) - (c.Count * 4 * c.listSize));

                for (k = c.listSize; k > 0; k--)
                {
                    InsertBytes(startof(c) + 4 + ((c.Count * 4) * k) + (extraStateBytes), 4 * (newCount - c.Count), 0);
                    Console.WriteLine("inserting {0} bytes at {1} for +{2} new items", 4 * (newCount - c.Count), startof(c) + 4 + (c.Count * 4) * k, newCount - c.Count);
                }
                if (padding > 0)
                    InsertBytes(RSZOffset + addedSz, padding, 0);
                ShowRefreshMessage("");
            }
            c.Count = newCount;
        }
    }
}
