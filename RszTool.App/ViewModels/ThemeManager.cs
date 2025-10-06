using PropertyChanged;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RszTool.App.ViewModels
{
    public class ThemeManager : INotifyPropertyChanged
    {
        private ThemeE ThemeN;
        private static ThemeManager? instance;
        public readonly SolidColorBrush LightForeground =
            Application.Current.Resources["LightForeground"] as SolidColorBrush ?? Brushes.Black;
        public readonly SolidColorBrush DarkForeground =
            Application.Current.Resources["DarkForeground"] as SolidColorBrush ?? Brushes.White;
        public readonly SolidColorBrush WinUIForeground =
            Application.Current.Resources["DarkForeground"] as SolidColorBrush ?? Brushes.White;
        public readonly SolidColorBrush MakaroForeground =
            Application.Current.Resources["MakaroForeground"] as SolidColorBrush ?? Brushes.Black;

        public static ThemeManager Instance
        {
            get => instance ??= new(ThemeE.light);
            private set => instance = value;
        }

        // 加载指定主题的资源字典
        private ThemeE LoadThemeResources
        {
            get => ThemeN;
            set
            {
                ThemeN = value;
                var mergedDicts = Application.Current.Resources.MergedDictionaries;
                mergedDicts.Clear();

                // 根据主题枚举加载对应资源文件
                string themePath = ThemeN switch
                {
                    ThemeE.dark => "Themes/DarkTheme.xaml",
                    ThemeE.Winui => "Themes/WinUITheme.xaml",
                    ThemeE.Makaro => "Themes/MakaroTheme.xaml",
                    _ => "Themes/LightTheme.xaml"
                };


                mergedDicts.Add(new ResourceDictionary
                {
                    Source = new Uri(themePath, UriKind.Relative)
                });

                PropertyChanged?.Invoke(this, new(nameof(ThemeN)));
            }
        }

       

        public event PropertyChangedEventHandler? PropertyChanged;

    private ThemeManager(ThemeE UserTheme)
    {
        LoadThemeResources = UserTheme;

        var resources = Application.Current.Resources;
        LightForeground = resources["LightForeground"] as SolidColorBrush ?? Brushes.Black;
        DarkForeground = resources["DarkForeground"] as SolidColorBrush ?? Brushes.White;
        WinUIForeground = resources["WinUIForeground"] as SolidColorBrush ?? Brushes.Gray; 
        MakaroForeground = resources["MakaroForeground"] as SolidColorBrush ?? Brushes.Black;
        }


        public static void Init(ThemeE UserTheme)
        {
           
            Instance = new(UserTheme);
        }
    }
}
