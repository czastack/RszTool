using System.Windows;

namespace RszTool.App.Common
{
    public static class MessageBoxUtils
    {
        public static MessageBoxResult ShowOk(string message, MessageBoxImage icon)
        {
            return MessageBox.Show(App.Current.MainWindow, message, "提示", MessageBoxButton.OK, icon);
        }

        public static MessageBoxResult Error(string message)
        {
            return ShowOk(message, MessageBoxImage.Error);
        }

        public static MessageBoxResult Warning(string message)
        {
            return ShowOk(message, MessageBoxImage.Warning);
        }

        public static bool Confirm(string message)
        {
            return MessageBox.Show(App.Current.MainWindow, message, "提示", MessageBoxButton.OKCancel) == MessageBoxResult.OK;
        }

        public static MessageBoxResult YesNoCancel(string message)
        {
            return MessageBox.Show(App.Current.MainWindow, message, "提示", MessageBoxButton.YesNoCancel);
        }
    }
}
