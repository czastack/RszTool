using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using RszTool.Common;

namespace RszTool.App
{
    public class SaveData
    {
        public const string JsonPath = "RszTool.App.SaveData.json";

        [JsonConverter(typeof(EnumJsonConverter<GameName>))]
        public GameName GameName { get; set; }
        public ObservableCollection<string> RecentFiles { get; set; } = new();
        public ContextIDData LastContextID { get; set; } = new();
    }


    public class ContextIDData : INotifyPropertyChanged
    {
        public int Group { get; set; }
        public int Index { get; set; }

        public string Text => $"Last ContextID Index: {Index} Group: {Group}";

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
