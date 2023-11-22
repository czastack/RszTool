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
                _ => 71,
            };
            RszParser = RszParser.GetInstance($"rsz{gameName}.json");
            PostInit();
        }

        public void PostInit()
        {
            if (GameName == GameName.re4)
            {
                var Null = RszParser.GetRSZClass(0);
                if (Null != null)
                {
                    Null.name = "NULL";
                }
                var GameObject = RszParser.GetRSZClass("via.GameObject");
                if (GameObject?.GetField("v4") is RszField v4 && v4.type == RszFieldType.Data && v4.size == 4)
                {
                    v4.type = RszFieldType.F32;
                }
            }
        }
    }
}
