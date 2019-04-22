using System.Windows.Controls;

namespace PhotoLabel.Wpf.Controls
{
    public class ListView : System.Windows.Controls.ListView
    {

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            // auto scroll to the selected item
            if (e.AddedItems.Count > 0) ScrollIntoView(e.AddedItems[0]);

            base.OnSelectionChanged(e);
        }
    }
}