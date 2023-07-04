using System.Text.RegularExpressions;
using uint64 = System.UInt64;
using int64 = System.Int64;

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

        uint RSZOffset;
        uint BHVTStart;
        int UVARStart;
        uint lastVarEnd;
        uint realStart = uint.MaxValue;
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
                uint maxVars = ((FileSize() - RSZOffset) / 6);
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

            ushort maxLevels;
            ulong[] RSZAddresses = new ulong[6000];
            h = FindFirst(5919570, 1, 0, 0, 0.0, 1, 0, 0, 24) + 4;
            while (h != 3)
            {
                RSZAddresses[maxLevels] = h - 4;
                maxLevels++;
                h = FindFirst(5919570, 1, 0, 0, 0.0, 1, h, 0, 24) + 4;
            }
            uint RSZFileMaxDivs = maxLevels / 100 + 1;
            uint[] RSZFileWaypoints = new uint[RSZFileMaxDivs];
            uint RSZFileDivCounter;

            if (AutoDetectGame) {

                if (RTVersion && (filename.Contains("scn.1[89]") || filename.Contains("pfb.16") || filename.Contains("motfsm2.30")
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
                RTVersion = (ReadUInt((ulong)(firstGameObj + 4)) == 216572408); //check if CRC version is new
            }
            else if (firstFolder != -1)
            {
                RTVersion = (ReadUInt((ulong)(firstFolder + 4)) == 2121287109);
            }
            else if (firstFSM != -1)
            {
                RTVersion = (ReadUInt((ulong)(firstFSM + 4)) == 1025596507 && Regex.IsMatch(filename, "motfsm2\\.36$") == false);
            }
            else if (firstRCOL != -1)
            {
                RTVersion = ((ReadUInt((ulong)(firstRCOL + 4)) == 374943849) && Regex.IsMatch(filename, "rcol\\.11$") == false);
            }
        }

        void AutoDetectVersion() {
            string hashName;
            uint checkedVersions, instanceCount, objectCount, hash, zz, varsChecked;
            bool origRTVersion = RTVersion;
            int badCRCs;
            string origVersion = RSZVersion, origExtractedDir = (string)extractedDir, origXFmt = xFmt,
                origLocal_Directory = Local_Directory, origJsonPath = JsonPath;

            FSeek(RSZOffset);
            if (FTell() + 12 < FileSize())
                instanceCount = ReadUInt(FTell() + 12), objectCount = ReadUInt(FTell() + 8);
            if (instanceCount) {
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

        private ulong FileSize()
        {
            return (ulong)fileStream.Length;
        }

        private void FSeek(uint64 tell)
        {
            fileStream.Position = (long)tell;
        }

        private float ReadFloat(uint64 tell)
        {
            FSeek(tell);
            return reader.ReadSingle();
        }

        private uint ReadUInt(uint64 tell)
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

        private byte ReadUByte(uint64 tell)
        {
            FSeek(tell);
            return reader.ReadByte();
        }

        private sbyte ReadByte()
        {
            return reader.ReadSByte();
        }

        private sbyte ReadByte(uint64 tell)
        {
            FSeek(tell);
            return reader.ReadSByte();
        }

        private ushort ReadUShort()
        {
            return reader.ReadUInt16();
        }

        private ushort ReadUShort(uint64 tell)
        {
            FSeek(tell);
            return reader.ReadUInt16();
        }

        private void ReadBytes(byte[] buffer, int64 pos, int n)
        {
            FSeek((uint)pos);
            fileStream.Read(buffer, 0, n);
        }

        private ulong FTell()
        {
            return (ulong)fileStream.Position;
        }

        private void FSkip(long skip)
        {
            fileStream.Seek(skip, SeekOrigin.Current);
        }

        bool detectedColorVector(uint64 tell)
        {
            if (tell + 16 <= FileSize())
            {
                float R = ReadFloat(tell), G = ReadFloat(tell + 4), B = ReadFloat(tell + 8), A = ReadFloat(tell + 12);
                return ((R >= 0 && G >= 0 && B >= 0 && (A == 0 || A == 1)) && ((R + G + B + A <= 4) || (R + G + B + A) % 1.0 == 0));
            }
            return false;
        }

        float readColorFloat(uint64 tell)
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

        bool detectedFloat(uint64 offset)
        {
            if (offset + 4 <= FileSize())
            {
                float flt = ReadFloat(offset);
                if (BHVTStart != -1)
                    return (ReadUByte(offset + 3) < 255 && (Abs(flt) > 0.000001 && Abs(flt) < 100000) || ReadInt(offset) == 0);
                else return (ReadUByte(offset + 3) < 255 && (Abs(flt) > 0.0000001 && Abs(flt) < 10000000) || ReadInt(offset) == 0);
            }
            return false;
        }

        bool detectedStringSm(uint64 offset)
        {
            if (offset + 4 <= FileSize())
                if (ReadUShort(offset - 2) == 0)
                    if (ReadByte(offset) != 0 || ReadUShort(offset) == 0)
                        if (ReadByte(offset + 1) == 0 || sizeof(ReadWString(offset)) > 5)
                            //if (sizeof(ReadWString(offset)) >= 2)
                                return true;
            return false;
        }

        bool detectedString(uint64 offset)
        {
            if (offset + 6 <= FileSize())
                if (ReadByte(offset) != 0 && ReadByte(offset + 1) == 0)
                    if (ReadByte(offset + 2) != 0 && ReadByte(offset + 3) == 0)
                        if (ReadByte(offset + 4) != 0) // && ReadByte(offset + 5) == 0
                            return true;
            return false;
        }

        bool detectedNode(uint tell)
        {
            if (tell + 12 < FileSize())
                if (ReadInt(tell - 4) == 0)
                    if (ReadInt(tell) != -1)
                        if (detectedHash(tell))
                            if (ReadInt(tell + 8) != 0)
                                if (detectedStringSm(startof(Header.BHVT.mNamePool) + 4 + (ReadUInt(tell + 8) * 2)))
                                    return true;
            return false;
        }
    }
}
