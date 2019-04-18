using System;
using System.Windows;
using System.Windows.Interactivity;

namespace PhotoLabel.Wpf
{
    public class RoutedEventTrigger : EventTriggerBase<DependencyObject>
    {
        #region variables

        #endregion

        protected override string GetEventName()
        {
            return RoutedEvent.Name;
        }

        protected override void OnAttached()
        {
            var associatedElement = AssociatedObject as FrameworkElement;

            if (AssociatedObject is Behavior behaviour)
            {
                associatedElement = ((IAttachedObject)behaviour).AssociatedObject as FrameworkElement;
            }

            if (associatedElement == null)
            {
                throw new ArgumentException($"{nameof(RoutedEventTrigger)} can only be associated to framework elements");
            }

            if (RoutedEvent != null) associatedElement.AddHandler(RoutedEvent, new RoutedEventHandler(OnRoutedEvent));
        }

        private void OnRoutedEvent(object sender, RoutedEventArgs args)
        {
            OnEvent(args);
        }

        public RoutedEvent RoutedEvent { get; set; }
    }
}