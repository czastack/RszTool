using RszTool.Common;

namespace RszTool.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // TestParseUser();
            TestParseUserRead();
            // TestParsePfb();
            // TestParsePfbRe2();
            // TestParsePfbRead();
            // TestParseScn();
            // TestParseScnRead();
            // TestScnExtractGameObjectRSZ();
            // TestScnExtractGameObjectToPfb();
            // TestImportGameObject();
            // TestParseMdf();
            // TestMurMur3Hash();
            // PrintEnums();
        }

        private static void TestRszParser()
        {
            TimerRecord record = new();
            record.Start("RszParser");
            RszParser parser = new("rszre4.json");
            record.End();
            Console.WriteLine(parser.GetRSZClassName(0x1001342f));
            Console.WriteLine(parser.GetFieldName(0x1004b6b4, 1));
        }

        static void TestParseUser()
        {
            string path = "test/AccessoryEffectSettingUserdata.user.2";
            string newPath = "test/AccessoryEffectSettingUserdata_new.user.2";
            RszFileOption option = new(GameName.re4);
            UserFile userFile = new(option, new FileHandler(path));
            userFile.Read();
            using FileHandler newFileHandler = new(newPath, true);
            userFile.WriteTo(newFileHandler);

            UserFile newUserFile = new(option, newFileHandler);
            newUserFile.Read(0);
            if (newUserFile.RSZ != null)
            {
                foreach (var item in newUserFile.RSZ.InstanceList)
                {
                    Console.WriteLine(item.Stringify());
                }
            }
        }

        static void TestParseUserRead()
        {
            string path = "test/AccessoryEffectSettingUserdata.user.2";
            RszFileOption option = new(GameName.re4);
            UserFile userFile = new(option, new FileHandler(path));
            userFile.Read();
            if (userFile.RSZ != null)
            {
                Console.WriteLine(userFile.RSZ.ObjectsStringify());
            }
        }

        static void TestParsePfb()
        {
            string path = "test/railcarcrossbowshellgenerator.pfb.17";
            string newPath = "test/railcarcrossbowshellgenerator_new.pfb.17";
            RszFileOption option = new(GameName.re4);
            PfbFile pfbFile = new(option, new FileHandler(path));
            pfbFile.Read();
            using FileHandler newFileHandler = new(newPath, true);
            pfbFile.WriteTo(newFileHandler);

            PfbFile newPfbFile = new(option, newFileHandler);
            newPfbFile.Read(0);

            if (newPfbFile.RSZ != null)
            {
                foreach (var item in newPfbFile.RSZ.InstanceList)
                {
                    Console.WriteLine(item.Stringify());
                }
            }
        }

        static void TestParsePfbRe2()
        {
            string path = "test/re2_rsz/em7400.pfb.16";
            string newPath = "test/re2_rsz/em7400_new.pfb.16";
            RszFileOption option = new(GameName.re2);
            PfbFile pfbFile = new(option, new FileHandler(path));
            pfbFile.Read();
            using FileHandler newFileHandler = new(newPath, true);
            pfbFile.WriteTo(newFileHandler);

            PfbFile newPfbFile = new(option, newFileHandler);
            newPfbFile.Read(0);

            if (newPfbFile.RSZ != null)
            {
                foreach (var item in newPfbFile.RSZ.InstanceList)
                {
                    Console.WriteLine(item.Stringify());
                }
            }
        }

        static void TestParsePfbRead()
        {
            string path = "test/railcarcrossbowshellgenerator.pfb.17";
            RszFileOption option = new(GameName.re4);
            PfbFile pfbFile = new(option, new FileHandler(path));
            pfbFile.Read();

            foreach (var item in pfbFile.UserdataInfoList)
            {
                Console.WriteLine(item.pathOffset.ToString("X"));
            }
            foreach (var item in pfbFile.RSZ!.RSZUserDataInfoList)
            {
                if (item is RSZUserDataInfo userDataInfo)
                {
                    Console.WriteLine(userDataInfo.pathOffset.ToString("X"));
                }
            }

            if (pfbFile.RSZ != null)
            {
                foreach (var item in pfbFile.RSZ.InstanceList)
                {
                    Console.WriteLine(pfbFile.RSZ.InstanceStringify(item));
                }
            }
        }

        static void TestParseScn()
        {
            string path = "test/gimmick_st66_101.scn.20";
            string newPath = "test/gimmick_st66_101_new.scn.20";
            RszFileOption option = new(GameName.re4);
            ScnFile scnFile = new(option, new FileHandler(path));
            scnFile.Read();
            using FileHandler newFileHandler = new(newPath, true);
            scnFile.WriteTo(newFileHandler);

            ScnFile newScnFile = new(option, newFileHandler);
            newScnFile.Read(0);

            if (newScnFile.RSZ != null)
            {
                foreach (var item in newScnFile.RSZ.InstanceList)
                {
                    Console.WriteLine(item.Stringify());
                }
            }
        }

        static void TestParseScnRead()
        {
            string path = "test/gimmick_st66_101.scn.20";
            RszFileOption option = new(GameName.re4);
            ScnFile scnFile = new(option, new FileHandler(path));
            scnFile.Read();

            if (scnFile.RSZ != null)
            {
                // Console.WriteLine(scnFile.RSZ.ObjectsStringify());
                scnFile.SetupGameObjects();
                if (scnFile.GameObjectDatas != null)
                {
                    foreach (var item in scnFile.GameObjectDatas)
                    {
                        Console.WriteLine(item.Name);
                    }
                }

            }
        }

        static void TestScnExtractGameObjectRSZ()
        {
            string path = "test/gimmick_st66_101.scn.20";
            RszFileOption option = new(GameName.re4);
            ScnFile scnFile = new(option, new FileHandler(path));
            scnFile.Read();

            if (scnFile.RSZ != null)
            {
                // Console.WriteLine(scnFile.RSZ.ObjectsStringify());
                scnFile.SetupGameObjects();
                FileHandler newFileHandler = new("test/gimmick_st66_101_new.rsz");
                RSZFile newRSZ = new(option, newFileHandler);
                bool success = scnFile.ExtractGameObjectRSZ("設置機銃砦１", newRSZ);
                Console.WriteLine(success);
            }
        }

        static void TestScnExtractGameObjectToPfb()
        {
            string path = "test/gimmick_st66_101.scn.20";
            string pfbPath = "test/gimmick_st66_101_new.pfb.17";
            RszFileOption option = new(GameName.re4);
            ScnFile scnFile = new(option, new FileHandler(path));
            scnFile.Read();
            scnFile.SetupGameObjects();
            PfbFile pfbFile = new(option, new FileHandler(pfbPath));
            bool success = scnFile.ExtractGameObjectToPfb("設置機銃砦１", pfbFile);
            if (success)
            {
                pfbFile.Write();
            }
            Console.WriteLine(success);
        }

        static void TestImportGameObject()
        {
            string path = "test/gimmick_st66_101.scn.20";
            string pathImportTo = "test/level_loc40_200.scn.20";
            string pathImportToNew = "test/level_loc40_200_new.scn.20";
            // string pathImportTo = "test/gimmick_st40_200_p001.scn.20";
            // string pathImportToNew = "test/gimmick_st40_200_p001_new.scn.20";
            RszFileOption option = new(GameName.re4);

            ScnFile scnFile = new(option, new FileHandler(path));
            scnFile.Read();
            scnFile.SetupGameObjects();
            ScnFile scnImportTo = new(option, new FileHandler(pathImportTo));
            scnImportTo.Read();
            scnImportTo.SetupGameObjects();
            var gameObject = scnFile.FindGameObject("設置機銃砦１");
            if (gameObject != null)
            {
                scnImportTo.ImportGameObject(gameObject);
                scnImportTo.WriteTo(new FileHandler(pathImportToNew));
            }
            else
            {
                Console.Error.WriteLine("GameObject not found");
            }
        }

        static void TestParseMdf()
        {
            string path = "test/cha200_20.mdf2.32";
            string newPath = "test/cha200_20_new.mdf2.32";
            RszFileOption option = new(GameName.re4);
            MdfFile mdfFile = new(option, new FileHandler(path));
            mdfFile.Read();
            using FileHandler newFileHandler = new(newPath, true);
            mdfFile.WriteTo(newFileHandler);

            // MdfFile newMdfFile = new(option, newFileHandler);
            // newMdfFile.Read(0);
        }

        static void TestMurMur3Hash()
        {
            string[] strings = {
                "Hair00_mat",
                "Hair01_mat",
                "Hair02_mat",
            };
            uint[] hashes = {
                0x430FD5EC,
                0x2A439950,
                0xFA9B78B1,
            };
            for (int i = 0; i < strings.Length; i++)
            {
                uint hash = PakHash.GetHash(strings[i]);
                string result = hash == hashes[i] ? "" : $", expacted {hashes[i]:X}";
                Console.WriteLine($"hash of {strings[i]} is {hash:X}{result}");
            }
        }

        static void PrintEnums()
        {
            RszFileOption option = new(GameName.re4);
            HashSet<string> enumSet = new();
            foreach (var rszClass in option.RszParser.ClassDict.Values)
            {
                foreach (var field in rszClass.fields)
                {
                    if (field.type is RszFieldType.S8 or RszFieldType.S16 or RszFieldType.S32 or RszFieldType.S64 or
                        RszFieldType.U8 or RszFieldType.U16 or RszFieldType.U32 or RszFieldType.U64 &&
                        field.original_type != "" && !field.original_type.StartsWith("System."))
                    {
                        enumSet.Add(field.original_type);
                    }
                }
            }
            foreach (var item in enumSet)
            {
                Console.WriteLine(item);
            }
        }
    }
}
