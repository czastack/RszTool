using ImGuiNET;

namespace RszTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // ImGuiSetup setup = new();
            // setup.SubmitUI += SubmitUI;
            // setup.Loop();

            TestModel();
        }

        private static void TestModel()
        {
            // string rszPath = @"/home/SENSETIME/chenzhenan/Downloads/gimmick_st66_105.scn.20";
            string rszPath = "test.bin";
            {
                using FileStream file = File.OpenWrite(rszPath);
                using BinaryWriter bw = new(file);
                bw.Write(1);
                bw.Write(1024);
                bw.Write(10.0f);
            }
            FileHandler handler = new(rszPath);
            DataClass clsTestItem = new DataClass("TestItem")
                .AddField<bool>("bool")
                .AddField<int>("int", align: 4)
                .AddField<float>("float");
            DataClass clsTest = new DataClass("Test")
                .AddArrayField("array", new ObjectType(clsTestItem), 1);
            var obj = new DataObject("test", 0, clsTest, handler);
            obj.ReadValues();
            // foreach (DataField field in cls.Fields)
            // {
            //     Console.WriteLine($"{field.Type} {field.Name} {field.Index} {field.Offset}");
            // }
            PrintDatas(obj);
        }

        const string Indent = "    ";
        private static void PrintDatas(IDataContainer container, int indent = 0)
        {
            foreach (var (field, value) in container.IterData())
            {
                for (int i = 0; i < indent; i++)
                {
                    Console.Write(Indent);
                }
                Console.WriteLine($"{field.Index} {field.Type} {field.Name} {field.Offset}, {value}");
                if (value is IDataContainer chlid)
                {
                    PrintDatas(chlid, indent + 1);
                }
            }
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
