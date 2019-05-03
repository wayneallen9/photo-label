using System.Windows;

namespace PhotoLabel.Wpf
{
    public static class AttachedProperties
    {
        public static DependencyProperty CloseProperty =
            DependencyProperty.RegisterAttached("Close",
                typeof(bool), typeof(AttachedProperties), new UIPropertyMetadata(false, (d, e) =>
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