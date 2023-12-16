using System.Text.Json;

namespace RszTool
{
    /// <summary>
    /// Rsz json parser
    /// </summary>
    public class EnumParser
    {
        private static readonly Dictionary<string, EnumParser> instanceDict = new();

        public static EnumParser GetInstance(string jsonPath)
        {
            if (!instanceDict.TryGetValue(jsonPath, out var rszParser))
            {
                rszParser = new EnumParser(jsonPath);
                instanceDict[jsonPath] = rszParser;
            }
            return rszParser;
        }

        public EnumDict EnumDict { get; }

        public EnumParser(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                using FileStream fileStream = File.OpenRead(jsonPath);
                EnumDict = new(JsonSerializer.Deserialize<Dictionary<string, EnumData>>(fileStream) ?? []);
            }
            else
            {
                EnumDict = new([]);
            }
        }
    }

    public record EnumDict(Dictionary<string, EnumData> Dict)
    {
        public EnumData? this[string name] => Dict.TryGetValue(name, out var data) ? data : null;
    }

    public record EnumData(EnumItem[] Items, bool Flags);
    public record EnumItem(string Name, ulong Value);
}
