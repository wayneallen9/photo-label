using System.Windows;

namespace PhotoLabel.Wpf.DependencyProperties
{
    public static class Close
    {
        public static DependencyProperty CloseProperty =
            DependencyProperty.RegisterAttached("Close",
                typeof(bool), typeof(Close), new UIPropertyMetadata(false, (d, e) =>
                {
                    // must be a boolean property attached to a window
                    if (!(d is Window w) || !(e.NewValue is bool newValue)) return;

                    // close the window when the property becomes true
                    if (!newValue) return;

                    w.DialogResult = true;
                    w.Close();
                }));

        public static bool GetClose(DependencyObject obj)
        {
            return (bool)obj.GetValue(CloseProperty);
        }

        public static void SetClose(DependencyObject obj, bool value)
        {
            obj.SetValue(CloseProperty, value);
        }
    }
}