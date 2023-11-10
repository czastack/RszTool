namespace RszTool
{
    public class RszFileOption
    {
        public string GameName { get; set; }
        public int TdbVersion { get; set; }
        public RszParser RszParser { get; set; }

        public RszFileOption(string gameName)
        {
            GameName = gameName;
            TdbVersion = gameName switch
            {
                "re4" => 71,
                _ => 71,
            };
            RszParser = RszParser.GetInstance($"rsz{gameName}.json");
        }
    }
}
