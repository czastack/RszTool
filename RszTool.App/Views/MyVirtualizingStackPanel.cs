using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            try
            {
                base.OnItemsChanged(sender, args);
            } catch {}
        }
    }
}
