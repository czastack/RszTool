using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RszTool.App.Common
{
    public static class AppUtils
    {
        public static void TryAction(Action action, [CallerMemberName] string name = "")
        {
            if (Debugger.IsAttached)
            {
                action();
            }
            else try
            {
                action();
            }
            catch (Exception e)
            {
                App.ShowUnhandledException(e, name);
            }
        }
    }
}
