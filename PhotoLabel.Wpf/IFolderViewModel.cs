using System.Collections.ObjectModel;

namespace PhotoLabel.Wpf
{
    public interface IFolderViewModel
    {
        string Name { get; }
        bool Exists { get; }
        bool IsHidden { get; }
        bool IsSelected { get; set; }
        string Path { get; set; }
        ObservableCollection<IFolderViewModel> SubFolders { get; set; }
    }
}