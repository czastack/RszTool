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
            string rszPath = @"C:\Users\An\Documents\Hack\Re\re4\RETool\re_chunk_000\natives\STM\_Chainsaw\Environment\Scene\Gimmick\st40\gimmick_st40_200_p100.scn.20";
            FileHandler handler = new(rszPath);
            DataClass cls = new();
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
