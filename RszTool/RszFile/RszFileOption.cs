namespace RszTool
{
    public class RszFileOption
    {
        public GameName GameName { get; set; }
        public int TdbVersion { get; set; }
        public RszParser RszParser { get; set; }

        public RszFileOption(GameName gameName)
        {
            GameName = gameName;
            TdbVersion = gameName switch
            {
                GameName.re4 => 71,
                GameName.re2 => 66,
                GameName.re2rt => 70,
                GameName.re3 => 68,
                GameName.re3rt => 70,
                GameName.re7 => 49,
                GameName.re8 => 69,
                GameName.sf6 => 71,
                _ => 71,
            };
            RszParser = RszParser.GetInstance($"rsz{gameName}.json");
        }
    }
}
