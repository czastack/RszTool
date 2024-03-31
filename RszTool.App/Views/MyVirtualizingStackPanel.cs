using System.Windows.Controls;

namespace RszTool.App.Views
{
    public class MyVirtualizingStackPanel : VirtualizingStackPanel
    {
        /// <summary>
        /// Publically expose BringIndexIntoView.
        /// </summary>
        public void BringIntoView(int index)
        {
            BringIndexIntoView(index);
        }

        protected override void OnClearChildren()
        {
            try
            {
                base.OnClearChildren();
            } catch {}
        }
    }
}
