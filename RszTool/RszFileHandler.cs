using System.Text.RegularExpressions;
using uint64 = System.UInt64;
using int64 = System.Int64;
using uint32 = System.UInt32;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if true
namespace RszTool
{
    public class RszFileHandler : FileHandler
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

        internal string RSZVersion        = "RE4"; // change between RE2, RE3, RE8, DMC5 or MHRise
        internal bool   RTVersion         = true;  // Use Ray-Tracing Update file formats for RE7, RE2R and RE3R (subject to AutoDetectGame)
        internal bool   Nesting           = true;  // Attempt to nest class instances inside eachother
        internal bool   ShowAlignment     = false; // Show metadata for each variable
        internal bool   ShowChildRSZs     = false; // Show all RSZs one after another, non-nested. Disabling hides nested RSZHeaders
        internal bool   UseSpacers        = true;  // Show blank rows between some structs
        internal bool   AutoDetectGame    = true;  // Automatically detect RSZVersion based on the name + ext of the file being viewed
        internal bool   RedetectBHVT      = true;  // Will automatically redetect the next BHVT node if there is a problem
        internal bool   HideRawData       = false; // Hides RawData struct
        internal bool   HideRawNodes      = true;  // Hides RawNodes struct
        internal bool   SortRequestSets   = true;  // Sorts RCOL RequestSets by their IDs
        internal bool   ExposeUserDatas   = true;  // Makes RSZFiles that contain embedded userDatas start after the Userdatas, for ctrl+J jump
        internal bool   SeekByGameObject  = false; // Will automatically seek between detected GameObjects to fix reading errors
        internal bool   ReadNodeFullNames = true;  // Reads FSM Node names with the names of all their parents (may be taxing)

        private RszParser rszParser;
        private string filename;
        private string Local_Directory;
        private string JsonPath;
        private string extractedDir;
        private string xFmt;

        public int RSZOffset;
        public int BHVTStart;
        public int UVARStart;
        internal int lastVarEnd;
        internal int realStart = -1;
        internal int level;
        internal bool finished;
        internal bool broken;
        internal byte silenceMessages;
        internal byte isAIFile;
        internal byte[] magic = new byte[4];
        internal ushort headerStringsCount;
        internal Dictionary<int, uint> headerStrings;
        internal uint[] dummyArr = new uint[1];
        // byte[] PasteBuffer = new byte[100000]; // 100KB buffer
        internal Dictionary<int, long> RSZAddresses;
        internal Dictionary<int, uint> RSZFileWaypoints;
        internal Dictionary<int, uint>? offs;
        internal Dictionary<int, uint>? aligns;
        internal Dictionary<int, uint>? sizes;
        internal uint RSZFileDivCounter;
        internal object? userDataPath;

        public RszFileHandler(string filename) : base(filename)
        {
            // variables:
            int i, j, k, m, n, o, h, temp;
            int matchSize, lastGameObject;
            int hashesLen;
            int noRetry;

            /*
            int64 FindFirst(
                <datatype> data,
                int matchcase=true,
                int wholeword=false,
                int method=0,
                double tolerance=0.0,
                int dir=1,
                int64 start=0,
                int64 size=0,
                int wildcardMatchLength=24)
            */
            RSZOffset = (int)FindFirst("RSZ");
            BHVTStart = (int)FindFirst("BHVT");
            UVARStart = BHVTStart;
            // local uint headerStrings[1 + RSZOffset / 64] <hidden=true>;
            headerStrings = new();

            if (detectedHash(4) && !detectedHash(0))
                ReadBytes(magic, 4, 4);
            else
                ReadBytes(magic, 0, 4);

            if (ShowAlignment)
            {
                // int varLen = 0;
                long maxVars = ((FileSize() - RSZOffset) / 6);
                if (maxVars > 1000000)
                    maxVars = 1000000;
                offs = new();
                aligns = new();
                sizes = new();
            }
            else
            {
                // int varLen = 0;
            }

            this.filename = filename;
            extractedDir = DMC5Path;
            Local_Directory = Path.GetDirectoryName(filename)!;
            uint findValue = (uint)Local_Directory.IndexOf("natives");
            Local_Directory = Local_Directory.Remove((int)findValue, Local_Directory.Length - (int)findValue) + "natives\\";
            string dir = Local_Directory.ToLower();

            ushort maxLevels = 0;
            RSZAddresses = new();
            h = (int)FindFirst(5919570) + 4;
            while (h != 3)
            {
                RSZAddresses[maxLevels] = h - 4;
                maxLevels++;
                h = (int)FindFirst(5919570, new() { start = h }) + 4;
            }
            int RSZFileMaxDivs = maxLevels / 100 + 1;
            // local uint RSZFileWaypoints[RSZFileMaxDivs] <hidden=true>;
            RSZFileWaypoints = new();

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
            rszParser = RszParser.GetInstance(JsonPath);

            if (RSZOffset > -1 && AutoDetectGame && !(RSZVersion == "RE7" && !RTVersion))
                AutoDetectVersion(dir);

            RTVersion = (RSZVersion == "RE2" || RSZVersion == "RE7" || RSZVersion == "RE3") ? RTVersion : false;

            string GameVersion = RSZVersion;
            bool IsRayTracing = RTVersion;
        }

        void CheckHashesForRT()
        {
            int firstGameObj = (int)FindFirst(3372393495); //via.GameObject
            int firstFolder = (int)FindFirst(2929908172);  //via.Folder
            int firstFSM = (int)FindFirst(4193703126);     //via.motion.Fsm2ActionPlayMotion
            int firstRCOL = (int)FindFirst(4150774079);     //via.physics.UserData

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

        // Start of reading File
        public void StartRead()
        {

        }

        void AutoDetectVersion(string dir) {
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

                            JsonPath = (Path.GetDirectoryName(filename) + "rsz" + RSZVersion).ToLower();
                            if (RTVersion && (RSZVersion == "RE2" || RSZVersion == "RE3" || RSZVersion == "RE7"))
                                JsonPath = JsonPath + "rt";
                            JsonPath = JsonPath + ".json";
                            Local_Directory = dir + xFmt;
                            rszParser = RszParser.GetInstance(JsonPath);
                            hashName = ReadHashName(hash);
                            checkedVersions++;
                        }
                        if (hashName == "Unknown Class!") { //checkedVersions == 8 &&
                            RSZVersion = origVersion; extractedDir = origExtractedDir; xFmt = origXFmt;
                            Local_Directory = origLocal_Directory; JsonPath = origJsonPath; RTVersion = origRTVersion;
                        } else {
                            Console.WriteLine($"RSZVersion auto detected to {RSZVersion}");
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

        int detectedGuid(long tell) {
            int zerosCount = 0;
            for (int o=0; o<16; o++) {
                if (ReadUByte(FTell()+o) == 0) zerosCount++;
            }
            return zerosCount;
        }

        bool detectedObject(long tell, int idx) {
            if (tell+4 <= FileSize()) {
                int test = ReadInt(tell);
                if (test < idx && test > 0 && (test > idx - 100 || userDataPath != null)) // && test > 2
                    return true;
            }
            return false;
        }

        void redetectObject(int idx) {
            if (!finished && broken) {
                long pos = FTell();
                while(FTell() <= FileSize() - 4) {
                    if (detectedObject(FTell(), idx)) {
                        // SetForeColor(cYellow);
                        Console.WriteLine($"Redetected object from {pos} to {FTell()}");
                        break;
                    } else FSkip(4);
                }
            }
        }

        void setAsBroken() {
            FSkip(-1);
            broken = true;
            // SetForeColor(cNone);
            // ubyte blank <hidden=true, bgcolor=cRed>;
        }

        void redetectFloat() {
            if (broken && FTell() + 4 <= FileSize() && (broken && !finished)) {
                long pos = FTell();
                while(FTell() <= FileSize() - 4 && detectedFloat(FTell()))
                    FSkip(4);

                if (FTell() != pos && FTell() < pos + 16) {
                    broken = false;
                    // SetForeColor(cYellow);
                    Console.WriteLine($"Redetected float from {pos} to {FTell()}");
                } else FSeek(pos);
            }
            if (!detectedFloat(FTell()) && ReadFloat(FTell()) != 0) {
                broken = false;
                // SetForeColor(cNone);
            }
        }

        void redetectGuid() {
            if (FTell() + 16 <= FileSize() && !finished && (detectedGuid(FTell()) >= 4)) { // && broken
                long pos = FTell();
                //if (broken)
                //    FSkip(-12);
                while(FTell() <= FileSize() - 16) {
                    if (detectedGuid(FTell()) == 16 || (detectedGuid(FTell()) < 4 && (detectedGuid(FTell()) <= detectedGuid(FTell() + 8)))) {
                        if (pos != FTell()) {
                            broken = false;
                            // SetForeColor(cYellow);
                            Console.WriteLine($"Redetected GUID from {pos} to {FTell()}");
                        }
                        break;
                    } else FSkip(8);
                }
            }
        }

        string ReadHashName(uint32 hash) {
            return rszParser.GetRSZClassName(hash);
        }

        string ReadHASHWithName(uint32 hash) {
            return $"{hash:X} -- {rszParser.GetRSZClassName(hash)}";
        }

        bool isValidString(long tell)
        {
            long alignedOffs = getAlignedOffset(tell, 4);
            if (alignedOffs + 4 >= FileSize())
                return false;
            uint size = ReadUInt(alignedOffs);
            if (ReadWStringLength(alignedOffs + 4) == 0)
                return false;
            string text = ReadWString(alignedOffs + 4);
            return (alignedOffs + 8 <= FileSize() && ReadUInt64(alignedOffs) == 1 || size == 0 ||
                (size == text.Length / 2 && ReadUByte(alignedOffs + 7) != 0));
        }

        void redetectStringBehind() {
            long pos = FTell();
            if (detectedString(FTell())) {
                while (detectedString(FTell()) && ReadUInt(FTell()-4) != ReadWStringLength(FTell()) / 2)
                    FSkip(-2);
                FSkip(-4);
                if (pos == FTell() || !isValidString(FTell()) || (ReadWStringLength(FTell()+4) + FTell() <= pos) ) {
                    //Printf("Aborting string redetection from %u to %u\n",  pos, FTell());
                    FSeek(pos);
                    setAsBroken();
                } else if (FTell() < pos) {
                    long newPos = FTell();
                    FSeek(pos);
                    // BLANK blank <read=ReadErrorNotice, bgcolor=cRed>;
                    FSeek(newPos);
                    // SetForeColor(cYellow);
                    broken = false;
                    Console.WriteLine($"Redetected string from {pos} back to {FTell()}");
                }
            }
        }

        void redetectString() {
            if (!broken && !isValidString(FTell()+4))
                return;
            if  (FTell() + 4 <= FileSize() && ( !finished && (broken || !isValidString(FTell()) ) ) ) {
                long pos = FTell();
                while(FTell() <= FileSize() - 4 && FTell() - 24 < pos) {
                    if (((detectedString(FTell()) && isValidString(FTell()-4)))) {
                        FSkip(-4);
                        break;
                    } else {
                        // uint skip <hidden=true>; //fgcolor=cRed,
                    }
                }
                if (FTell() - pos > 16 && broken) {
                    FSeek(pos); //abort
                } else if (FTell() - pos > 8 && !broken) {
                    FSeek(pos); //abort
                } else {
                    long newPos = FTell();
                    FSeek(pos);
                    // BLANK blank <read=ReadErrorNotice, bgcolor=cRed>; FSeek(newPos);
                    // SetForeColor(cYellow);
                    broken = false;
                    Console.WriteLine($"Redetected string from {pos} to {FTell()}");
                }
            }
        }

        public long getAlignedOffset(long tell, int alignment) {
            long offset = tell;
            switch (alignment) {
                case 2:  offset = tell + (tell % 2); break;  // 2-byte
                case 4:  offset = (long)((ulong)(tell + 3) & 0xFFFFFFFFFFFFFFFC); break;  // 4-byte
                case 8:  offset = (long)((ulong)(tell + 7) & 0xFFFFFFFFFFFFFFF8); break;  // 8-byte
                case 16: offset = (long)((ulong)(tell + 15) & 0xFFFFFFFFFFFFFFF0); break; // 16-byte
                default: break;
            }
            return offset;
        }

        public void SeekOffsetAligned(int offset, int align = 4)
        {
            FSeek(getAlignedOffset(FTell() + offset, align));
        }

        // public void ForceWriteString(uint tell, uint maxSize, string str) {
        //     OverwriteBytes(tell, maxSize, 0);
        //     if (str != " " && str != "")
        //         WriteWString(tell, str);
        // }

        public void ShowRefreshMessage(string extraMsg) {
            // if (!silenceMessages)
            //     MessageBox( idOk, "Insert Data", "%sPress F5 to refresh the template and fix template results", extraMsg);
        }

        public void FixOffsets(long tell, long tellLimit, long insertPoint, long maxOffset, long addedSz, int doInt32)
        {
            if (tell > tellLimit)
            {
                Console.WriteLine("Cannot fix offsets: insert point {0} is before start boundary {1}", insertPoint, tell);
                return;
            }

            Console.WriteLine("Fixing Offsets greater than {0} and less than {1} from positions {2} to {3}:\n\n", insertPoint, maxOffset, tell, tellLimit);

            long pos = FTell();
            long tmp;
            int varSize = 8 + -4 * ((doInt32 > 0) ? 1 : 0);

            FSeek(tell);

            while (FTell() + varSize <= tellLimit)
            {
                if (FTell() + varSize > FileSize())
                    break;

                if (doInt32 != 0)
                    tmp = ReadInt(FTell());
                else
                    tmp = ReadInt64(FTell());

                if (tmp >= insertPoint && tmp <= maxOffset)
                {
                    Console.WriteLine("@ position {0}: {1} >= {2} (limit {3}) added +{4}", FTell(), tmp, insertPoint, maxOffset, addedSz);

                    if (doInt32 != 0)
                        WriteUInt(FTell(), (uint)(tmp + addedSz));
                    else
                        WriteInt64(FTell(), tmp + addedSz);
                }

                FSkip(varSize);
            }
        }

        // Sorts array in ascending order, then sorts array2 by array (from Che)
        void quicksort( int low, int high, uint[] array, uint[]? array2) {
            int i = low;
            int j = high;
            uint temp = 0;
            uint z = array[(low + high) / 2]; // Choose a pivot value
            while( i <= j ) { // Partition the data
                while( array[i] < z ) // Find member above
                    i++;
                while( array[j] > z ) // Find element below
                    j--;
                if( i <= j )  {
                    // swap two elements
                    temp     = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                    if (array2 != null) {
                        temp = array2[i];
                        array2[i] = array2[j];
                        array2[j] = temp;
                    }
                    i++;
                    j--;
                }
            }
            // Recurse
            if( low < j )
                quicksort( low, j, array, array2 );
            if( i < high )
                quicksort( i, high, array, array2 );
        }

        public int getLevel(uint offset) {
            int L = 0;
            for (L=getRSZFileWaypointIndex(offset); L<level; L++) {
                if (offset >= startof(RSZFile[L].Data) && offset < startof(RSZFile[L].Data) + Unsafe.SizeOf(RSZFile[L].Data))
                    break;
            }
            return L;
        }

        public int getLevelRSZ(uint offset) {
            int L;
            for (L=getRSZFileWaypointIndex(offset); L<level; L++) {
                if (offset >= startof(RSZFile[L].RSZHeader) - 16 && offset <= startof(RSZFile[L].RSZHeader) + 16)
                    break;
            }
            return L;
        }

        /*
        struct rGUID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] uuid;

            // The following fields are hidden and their values are obtained through ReadrGUID and ReadrGUIDComment functions

            [MarshalAs(UnmanagedType.I4)]
            public int firstFourBytes;

            [MarshalAs(UnmanagedType.LPStr)]
            public string Guid;

            [MarshalAs(UnmanagedType.LPStr)]
            public string GameObjectRef;
        }

        void FixOffsets(rGUID guid)
        {
            if (guid.firstFourBytes != -1 && guid.firstFourBytes != 0)
            {
                if (FindFirst(guid.firstFourBytes) != startof(guid.uuid) || FindFirst(guid.firstFourBytes, 1, 0, 0, 0.0, 1, startof(guid.uuid) + 16, 0, 24) != -1)
                {
                    string translatedGuid = TranslateGUID(guid.uuid);
                    FSkip(-4);
                    SAMEGUIDS sameGuids = new SAMEGUIDS(translatedGuid);
                }
            }
        }

        string TranslateGUID(byte[] uuid)
        {
            char[] s = new char[37];
            SPrintf(s,
                "{0:x2}{1:x2}{2:x2}{3:x2}-{4:x2}{5:x2}-{6:x2}{7:x2}-{8:x2}{9:x2}-{10:x2}{11:x2}{12:x2}{13:x2}{14:x2}{15:x2}",
                uuid[3], uuid[2], uuid[1], uuid[0], uuid[5], uuid[4], uuid[7], uuid[6],
                uuid[8], uuid[9], uuid[10], uuid[11], uuid[12], uuid[13], uuid[14], uuid[15]
            );
            return new string(s);
        }

        string ReadrGUID(rGUID g)
        {
            string Guid = TranslateGUID(g.uuid);
            return Guid;
        }

        string ReadrGUIDComment(rGUID g)
        {
            if (exists(g.GameObjectRef))
                return g.GameObjectRef;
            return "";
        }

        void WriterGuid(ref rGUID g, string s)
        {
            byte offset = 0;
            byte[] uuid = new byte[16];
            for (byte ii = 0; ii < 16; ii++)
            {
                if (ii == 4 || ii == 6 || ii == 8 || ii == 10)
                    offset++;
                SScanf(SubStr(s, ii * 2 + offset, 2), "%x", out uuid[ii]);
            }
            byte[] uuid_le = new byte[16]
            {
                uuid[3], uuid[2], uuid[1], uuid[0], uuid[5], uuid[4], uuid[7], uuid[6],
                uuid[8], uuid[9], uuid[10], uuid[11], uuid[12], uuid[13], uuid[14], uuid[15]
            };
            WriteBytes(uuid_le, startof(g), 16);
        }
         */

         //functions for opening files:
        string getRE2ext(string ext) {
            switch (ext) {
                case ".jcns": return ".11";
                case ".pfb": return ".16";
                case ".mdf2": case ".tex": case ".rcol": case ".jmap": return ".10";
                case ".efx": return ".1769669";
                case ".wcc": case ".wss": case ".ies": case ".uvar": case ".fbxskel": return ".2";
                case ".wel":   return ".11";
                case ".mesh": return ".1808312334";
                case ".fsmv2": case ".bhvt": case ".motfsm2": return ".30";
                case ".scn": return ".19";
                case ".motbank": case ".mov": return ".1";
                case ".chain": return ".21";
                case ".lprb": return ".3";
                case ".mmtr": return ".1808160001";
                case ".tml": case ".clip": case ".rbs": return ".27";
                case ".motlist": return ".85";
                case ".mcol": return ".3017";
                case ".cfil": case ".uvs":  return ".7";
                case ".mot": return ".65";
                case ".gui": return ".270020";
                case ".rmesh": return ".10008";
                case ".rtex": return ".4";
                /* case ".rbs": */ case ".rdd": return ".27019";
                case ".mcamlist": case ".msg": return ".13";
                default: return "";
            }
        }

        string getRE3ext(string ext) {
            switch (ext) {
                case ".mcol": return ".9018";
                case ".jcns": return ".12";
                case ".pfb": return ".17";
                case ".mdf2": return ".13";
                case ".tex": return ".190820018";
                case ".rcol": case ".jmap": return ".11";
                case ".lprb": return ".4";
                case ".efx": return ".2228526";
                case ".wcc": case ".wss": case ".ies": case ".uvar": case ".user": return ".2";
                case ".wel":   return ".11";
                case ".mesh": return ".1902042334";
                case ".fsmv2": case ".bhvt": case ".tml": case ".clip": return ".34";
                case ".motfsm2": return ".36";
                case ".scn": return ".20";
                case ".mov": return ".1";
                case ".chain": return ".24";
                case ".fbxskel": case ".motbank":  return ".3";
                case ".mmtr": return ".1905100741";
                case ".rbs": return ".28";
                case ".motlist": return ".99";
                case ".cfil": case ".uvs":  return ".7";
                case ".mot": return ".78";
                case ".gui": return ".340020";
                case ".rmesh": return ".17008";
                case ".rtex": return ".4";
                case ".rdd": return ".28019";
                case ".mcamlist": return ".14";
                case ".msg": return ".15";
                default: return "";
            }
        }

        string getRE7ext(string ext) {
            switch (ext) {
                case ".uvar": case ".lprb": case ".rcol": case ".mcol": case ".wcc": case ".wss": case ".ies": return ".2";
                case ".pfb": return ".16";
                case ".mdf2": return ".6";
                case ".efx": return ".1179750";
                case ".tex": case ".jmap": case ".aimap": return ".8";
                case ".wel":   return ".10";
                case ".mesh": return ".32";
                case ".fsm": case ".motfsm": return ".17";
                case ".scn": case ".tml": return ".18";
                case ".motbank": return ".1";
                case ".mov": return ".1.x64";
                case ".chain": return ".5";
                case ".mmtr": return ".69";
                case ".motlist": return ".60";
                case ".cfil": return ".3";
                case ".uvs":  return ".5";
                case ".mot": return ".17";
                case ".gui": return ".180014";
                case ".rtex": return ".4";
                case ".mcamlist": return ".7";
                case ".msg": return ".12";
                case ".clo": return ".2016100701";
                case ".rbd": case ".rdl": return ".2016100700";
                default: return "";
            }
        }

        string getRE8ext(string ext) {
            switch (ext) {
                case ".mcol": return ".9018";
                case ".jcns": return ".16";
                case ".pfb": return ".17";
                case ".mdf2": return ".19";
                case ".tex": return ".30";
                case ".rcol": return ".18";
                case ".jmap": return ".17";
                case ".lprb": return ".4";
                case ".efx": return ".2228526";
                case ".wcc": case ".wss": case ".ies": case ".uvar": case ".user": return ".2";
                case ".wel":   return ".11";
                case ".mesh": return ".2101050001";
                case ".bhvt": case ".tml": return ".34";
                case ".fsmv2": case ".clip": return ".40";
                case ".motfsm2": return ".42";
                case ".scn": return ".20";
                case ".mov": case ".finf": return ".1";
                case ".chain": return ".39";
                case ".fbxskel": case ".motbank":  return ".3";
                case ".mmtr": return ".1905100741";
                case ".rbs": return ".28";
                case ".motlist": return ".486";
                case ".cfil": case ".uvs":  return ".7";
                case ".mot": return ".458";
                case ".gui": return ".400023";
                case ".rmesh": return ".17008";
                case ".rtex": return ".5";
                case ".rdd": return ".28019";
                case ".mcamlist": return ".17";
                case ".msg": return ".15";
                case ".gpuc": return ".62";
                default: return "";
            }
        }

        string getSF6ext(string ext) {
            switch (ext) {
                case ".mesh": return ".220721329";
                case ".mdf2": return ".31";
                case ".scn": return ".20";
                case ".user": return ".2";
                case ".pfb": return ".17";
                default: return "";
            }
        }

        string getMHRext(string ext) {
            switch (ext) {
                case ".mcol": return ".10019";
                case ".jcns": return ".14";
                case ".pfb": return ".17";
                case ".mdf2": return ".19";
                case ".tex": return ".28";
                case ".rcol": return ".18";
                case ".jmap": return ".16";
                case ".lprb": return ".4";
                case ".efx": return ".2621987";
                case ".wcc": case ".wss": case ".ies": case ".uvar": case ".user": return ".2";
                case ".wel":   return ".11";
                case ".mesh": return ".2008058288";
                case ".bhvt": case ".tml": return ".34";
                case ".fsmv2": case ".clip": return ".40";
                case ".motfsm2": return ".42";
                case ".scn": return ".20";
                case ".mov": case ".finf": return ".1";
                case ".chain": return ".35";
                case ".fbxskel": case ".motbank":  return ".3";
                case ".mmtr": return ".1905100741";
                case ".rbs": return ".28";
                case ".motlist": return ".486";
                case ".cfil": case ".uvs":  return ".7";
                case ".mot": return ".458";
                case ".gui": return ".400023";
                case ".rmesh": return ".17008";
                case ".rtex": return ".5";
                case ".rdd": return ".28019";
                case ".mcamlist": return ".17";
                case ".msg": return ".15";
                case ".gpuc": return ".62";
                case ".iklookat2": return ".8";
                default: return "";
            }
        }

        string getDMCext(string ext) {
            switch (ext) {
                case ".jcns": return ".11";
                case ".pfb": return ".16";
                case ".mdf2": case ".rcol": case ".jmap": return ".10";
                case ".efx": return ".1769672";
                case ".wcc": case ".wss": case ".ies": case ".uvar": return ".2";
                case ".wel": case ".tex":  return ".11";
                case ".mesh": return ".1808282334";
                case ".fsmv2": case ".bhvt": return ".30";
                case ".scn": return ".19";
                case ".motbank": case ".mov": return ".1";
                case ".chain": return ".21";
                case ".fbxskel": case ".lprb": return ".3";
                case ".motfsm2": return ".31";
                case ".mmtr": return ".1808168797";
                case ".tml": case ".clip": case ".rbs": return ".27";
                case ".motlist": return ".85";
                case ".mcol": return ".3017";
                case ".cfil": case ".uvs":  return ".7";
                case ".mot": return ".65";
                case ".gui": return ".270020";
                case ".rmesh": return ".10008";
                case ".rtex": return ".4";
                /* case ".rbs": */ case ".rdd": return ".27019";
                case ".mcamlist": case ".msg": return ".13";
                default: return "";
            }
        }

        // main typedef for RSZ chunks:



        //
        void fakeStateList() {
            int count = ReadInt();
            FSkip(4);
            if (count != 0 && FTell() + count * 4 < FileSize()) {
                FSkip(count * 4);
            }
        }

        //Murmur3 hash generation by Darkness:
        public static uint32 fmix32(uint32 h){
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }

        public static uint32 mmh3 (byte[] key, uint32 length , uint32 seed){
            uint64 block_start;
            uint nBlocks = length / 4;
            uint32 h1 = seed;

            const uint32 c1 = 0xcc9e2d51;
            const uint32 c2 = 0x1b873593;

            uint32 k1;
            for(block_start = 0; block_start < nBlocks * 4; block_start+= 4){
                k1 = (uint32)key[block_start + 3] << 24 |
                     (uint32)key[block_start + 2] << 16 |
                     (uint32)key[block_start + 1] << 8 |
                     (uint32)key[block_start + 0];

                k1 = (c1 * k1) & 0xFFFFFFFF;
                k1 = (k1 << 15 | k1 >> 17) & 0xFFFFFFFF;
                k1 = (c2 * k1) & 0xFFFFFFFF;

                h1 ^= k1;
                h1 = (h1 << 13 | h1 >> 19) & 0xFFFFFFFF;
                h1 = (h1 * 5 + 0xe6546b64) & 0xFFFFFFFF;
            }

            uint32 tail_index = nBlocks * 4;
            k1 = 0;
            uint32 tail_size = length & 3;

            if(tail_size >= 3)
                k1 ^= (uint32)key[tail_index + 2] << 16;
            if(tail_size >= 2)
                k1 ^= (uint32)key[tail_index + 1] << 8;
            if(tail_size >= 1)
                k1 ^= (uint32)key[tail_index + 0];

            if(tail_size > 0){
                k1 = (k1 * c1) & 0xFFFFFFFF;
                k1 = (k1 << 15 | k1 >> 17) & 0xFFFFFFFF;
                k1 = (k1 * c2) & 0xFFFFFFFF;
                h1 ^= k1;
            }

            return fmix32(h1 ^ length);
        }

        public static uint32 hash_wide(string key){
            int it;
            int length = key.Length * 2;
            if (length != 0) {
                byte[] key_array = new byte[length];
                for(it = 0; it < length; it += 2){
                    key_array[it] = (byte)key[it / 2];
                    key_array[it + 1] = 0;
                }
                return mmh3(key_array, (uint)length, 0xFFFFFFFF);
            } return 0;
        }

    }
}
#endif
