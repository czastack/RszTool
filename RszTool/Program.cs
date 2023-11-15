using RszTool.Common;
using ImGuiNET;

namespace RszTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TestParseUser();
            // TestParseUserRead();
            // TestParsePfb();
            // TestParsePfbRead();
            // TestParseScn();
            // TestParseScnRead();
            // TestScnExtractGameObjectRSZ();
            // TestScnExtractGameObjectToPfb();
            // TestParseMdf();
            // TestMurMur3Hash();
            // TestParseEnum();

            // ImGuiSetup setup = new();
            // setup.SubmitUI += SubmitUI;
            // setup.Loop();
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
            RszFileOption option = new("re4");
            UserFile userFile = new(option, new FileHandler(path));
            userFile.Read();
            using FileHandler newFileHandler = new(newPath);
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
            RszFileOption option = new("re4");
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
            RszFileOption option = new("re4");
            PfbFile pfbFile = new(option, new FileHandler(path));
            pfbFile.Read();
            using FileHandler newFileHandler = new(newPath);
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
            RszFileOption option = new("re4");
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
            RszFileOption option = new("re4");
            ScnFile scnFile = new(option, new FileHandler(path));
            scnFile.Read();
            using FileHandler newFileHandler = new(newPath);
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
            RszFileOption option = new("re4");
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
            RszFileOption option = new("re4");
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
            RszFileOption option = new("re4");
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

        static void TestParseMdf()
        {
            string path = "test/cha200_20.mdf2.32";
            string newPath = "test/cha200_20_new.mdf2.32";
            RszFileOption option = new("re4");
            MdfFile mdfFile = new(option, new FileHandler(path));
            mdfFile.Read();
            using FileHandler newFileHandler = new(newPath);
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

        [Flags]
        public enum TestEnum
        {
            None = 0,
            A = 1,
            B = 2,
            C = 4,
        }

        static void TestParseEnum()
        {
            string value = "A | B";
            TestEnum result = Enum.Parse<TestEnum>(value.Replace("|", ","));
            Console.WriteLine(result);
        }

        private static void SubmitUI()
        {
            // Demo code adapted from the official Dear ImGui demo program:
            // https://github.com/ocornut/imgui/blob/master/examples/example_win32_directx11/main.cpp#L172

            // 1. Show a simple window.
            // Tip: if we don't call ImGui.BeginWindow()/ImGui.EndWindow() the widgets automatically appears in a window called "Debug".
            {
                ImGui.Text("Hello, world!");
            }

            if (ImGui.TreeNode("Tabs"))
            {
                if (ImGui.TreeNode("Basic"))
                {
                    ImGuiTabBarFlags tab_bar_flags = ImGuiTabBarFlags.None;
                    if (ImGui.BeginTabBar("MyTabBar", tab_bar_flags))
                    {
                        if (ImGui.BeginTabItem("Avocado"))
                        {
                            ImGui.Text("This is the Avocado tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        if (ImGui.BeginTabItem("Broccoli"))
                        {
                            ImGui.Text("This is the Broccoli tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        if (ImGui.BeginTabItem("Cucumber"))
                        {
                            ImGui.Text("This is the Cucumber tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }
                    ImGui.Separator();
                    ImGui.TreePop();
                }
                ImGui.TreePop();
            }
        }
    }
}
