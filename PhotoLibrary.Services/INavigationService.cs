using System.Windows;

namespace PhotoLabel.Services
{
    public interface INavigationService
    {
        bool? ShowDialog<T>(object dataContext) where T : Window, new();
    }
}