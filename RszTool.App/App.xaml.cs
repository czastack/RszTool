using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using RszTool.App.Common;

namespace RszTool.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            SaveData? saveData = null;
            if (File.Exists(SaveData.JsonPath))
            {
                using FileStream fileStream = File.OpenRead(SaveData.JsonPath);
                saveData = JsonSerializer.Deserialize<SaveData>(fileStream);
                if (saveData != null) SaveData = saveData;
            }
            SaveData = saveData ?? new();
        }

        public static App Instance => (App)Current;
        public SaveData SaveData { get; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SwitchTheme(true);

            // 添加全局异常处理程序
            SetupUnhandledExceptionHandling();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            JsonUtils.DumpJson(SaveData.JsonPath, SaveData);
        }

        private void SwitchTheme(bool isDarkTheme)
        {
            var mergedDictionaries = Resources.MergedDictionaries;
            mergedDictionaries.Clear();
            mergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(
                isDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative) });
        }

        private void SetupUnhandledExceptionHandling()
        {
            // Catch exceptions from all threads in the AppDomain.
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                ShowUnhandledException((Exception)args.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException", false);

            // Catch exceptions from each AppDomain that uses a task scheduler for async operations.
            TaskScheduler.UnobservedTaskException += (sender, args) =>
                ShowUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException", false);

            // Catch exceptions from a single specific UI dispatcher thread.
            Dispatcher.UnhandledException += (sender, args) =>
            {
                // If we are debugging, let Visual Studio handle the exception and take us to the code that threw it.
                if (!Debugger.IsAttached)
                {
                    args.Handled = true;
                    ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException", true);
                }
            };

            // Catch exceptions from the main UI dispatcher thread.
            // Typically we only need to catch this OR the Dispatcher.UnhandledException.
            // Handling both can result in the exception getting handled twice.
            // Application.Current.DispatcherUnhandledException += (sender, args) =>
            // {
            //     // If we are debugging, let Visual Studio handle the exception and take us to the code that threw it.
            //     if (!Debugger.IsAttached)
            //     {
            //         args.Handled = true;
            //         ShowUnhandledException(args.Exception, "Application.Current.DispatcherUnhandledException", true);
            //     }
            // };
        }

        public static void ShowUnhandledException(Exception e, string unhandledExceptionType, bool promptUserForShutdown = false)
        {
            var messageBoxTitle = $"Unexpected Error Occurred: {unhandledExceptionType}";
            var messageBoxMessage = $"The following exception occurred:\n\n{e}";
            var messageBoxButtons = MessageBoxButton.OK;

            if (promptUserForShutdown)
            {
                messageBoxMessage += "\n\nShould we close it?";
                messageBoxButtons = MessageBoxButton.YesNo;
            }

            // Let the user decide if the app should die or not (if applicable).
            if (MessageBox.Show(Application.Current.MainWindow, messageBoxMessage, messageBoxTitle, messageBoxButtons) == MessageBoxResult.Yes &&
                promptUserForShutdown)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
