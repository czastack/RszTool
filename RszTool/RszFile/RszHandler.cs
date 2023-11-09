namespace RszTool
{
    public class RszHandler
    {
        public FileHandler FileHandler { get; set; }
        public RszParser RszParser { get; set; }
        public int TdbVersion { get; set; }
        public string GameName { get; set; }
    }
}
