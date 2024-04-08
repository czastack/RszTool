using PropertyChanged;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace RszTool.App.ViewModels
{
    public class ThemeManager : INotifyPropertyChanged
    {
        private bool isDarkTheme;
        private static ThemeManager? instance;
        public readonly SolidColorBrush LightForeground =
            Application.Current.Resources["LightForeground"] as SolidColorBrush ?? Brushes.Black;
        public readonly SolidColorBrush DarkForeground =
            Application.Current.Resources["DarkForeground"] as SolidColorBrush ?? Brushes.White;

        public static ThemeManager Instance
        {
            get => instance ??= new(false);
            private set => instance = value;
        }

        [DoNotCheckEquality]
        public bool IsDarkTheme
        {
            get => isDarkTheme;
            set
            {
                isDarkTheme = value;
                var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
                mergedDictionaries.Clear();
                mergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(isDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative)
                });
                PropertyChanged?.Invoke(this, new(nameof(IsDarkTheme)));
            }
        }

        public SolidColorBrush Foreground => IsDarkTheme ? DarkForeground : LightForeground;

        public event PropertyChangedEventHandler? PropertyChanged;

        private ThemeManager(bool isDarkTheme)
        {
            IsDarkTheme = isDarkTheme;
            var resources = Application.Current.Resources;
            LightForeground = resources["LightForeground"] as SolidColorBrush ?? Brushes.Black;
            DarkForeground = resources["DarkForeground"] as SolidColorBrush ?? Brushes.White;
        }

        public static void Init(bool isDarkTheme = false)
        {
            Instance = new(isDarkTheme);
        }
    }
}
