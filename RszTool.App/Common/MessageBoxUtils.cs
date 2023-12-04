using System.Windows;
using RszTool.App.Resources;

namespace RszTool.App.Common
{
    public static class MessageBoxUtils
    {
        public static MessageBoxResult ShowOk(string message, MessageBoxImage icon)
        {
            return MessageBox.Show(Application.Current.MainWindow, message, Texts.Message, MessageBoxButton.OK, icon);
        }

        public static MessageBoxResult Error(string message)
        {
            return ShowOk(message, MessageBoxImage.Error);
        }

        public static MessageBoxResult Info(string message)
        {
            return ShowOk(message, MessageBoxImage.Information);
        }

        public static MessageBoxResult Warning(string message)
        {
            return ShowOk(message, MessageBoxImage.Warning);
        }

        public static bool Confirm(string message)
        {
            return MessageBox.Show(Application.Current.MainWindow, message, Texts.Message, MessageBoxButton.OKCancel) == MessageBoxResult.OK;
        }

        public static MessageBoxResult YesNoCancel(string message)
        {
            return MessageBox.Show(Application.Current.MainWindow, message, Texts.Message, MessageBoxButton.YesNoCancel);
        }
    }
}
