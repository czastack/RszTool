using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;

namespace RszTool.App.Resources
{
    public class TextsManager : INotifyPropertyChanged
    {
        private static TextsManager? instance;
        public static TextsManager Instance => instance ??= new();
        public event PropertyChangedEventHandler? PropertyChanged;

        public TextsManager()
        {
        }

        public string? this[string name] => Texts.ResourceManager.GetString(name, Texts.Culture);

        public void ChangeLanguage(CultureInfo cultureInfo)
        {
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));  //字符串集合，对应资源的值
        }
    }
}