using System.Windows;
using Dragablz;

namespace RszTool.App.Views
{
    public class CustomInterTabClient : DefaultInterTabClient
    {
        public override TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
        {
            if (Application.Current.Windows.Count > 1)
            {
                return TabEmptiedResponse.CloseWindowOrLayoutBranch;
            }
            return TabEmptiedResponse.DoNothing;
        }
    }
}
