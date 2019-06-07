using PhotoLabel.Wpf.Annotations;
using Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PhotoLabel.Wpf
{
    public class FolderViewModel : INotifyPropertyChanged
    {
        #region variables

        private DirectoryInfo _directoryInfo;
        private bool _isSelected;
        private readonly ILogger _logger;
        private string _path;
        #endregion

        public FolderViewModel()
        {
            // save dependencies
            _logger = Injector.Get<ILogger>();
        }

        public string Caption => _directoryInfo?.Name;

        public bool Exists => _directoryInfo?.Exists ?? false;

        public bool IsHidden => _directoryInfo?.Attributes.HasFlag(FileAttributes.Hidden) ?? false;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(IsSelected)} has changed...");
                    if (_isSelected == value)
                    {
                        logger.Trace($"Value of {nameof(IsSelected)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting value of {nameof(IsSelected)} to {value}...");
                    _isSelected = value;

                    logger.Trace($"Bubbling value of {nameof(IsSelected)} down...");
                    foreach (var folderViewModel in SubFolders) folderViewModel.IsSelected = value;

                    OnPropertyChanged();
                }
            }
        }

        public void LoadSubFolders()
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Creating the collection to return...");
                var observableCollection = new ObservableCollection<FolderViewModel>();

                foreach (var subFolderPath in _directoryInfo.EnumerateDirectories())
                {
                    logger.Trace($@"Checking if ""{subFolderPath}"" is hidden...");
                    var subFolderViewModel = Injector.Get<FolderViewModel>();
                    subFolderViewModel.Path = subFolderPath.FullName;
                    if (!subFolderViewModel.Exists)
                    {
                        logger.Trace($@"""{subFolderPath}"" does not exist.  Skipping...");
                        continue;
                    }

                    if (subFolderViewModel.IsHidden)
                    {
                        logger.Trace($@"""{subFolderPath}"" is hidden.  Skipping...");
                        continue;
                    }

                    logger.Trace($@"Loading subfolders of ""{subFolderViewModel.Path}""...");
                    subFolderViewModel.LoadSubFolders();

                    logger.Trace($@"Adding ""{subFolderPath}"" to subfolders...");
                    observableCollection.Add(subFolderViewModel);

                    logger.Trace("Watching for property changes to subfolders...");
                    subFolderViewModel.PropertyChanged += SubFolderViewModel_PropertyChanged;
                }

                SubFolders = observableCollection;
            }
        }

        private void SubFolderViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Bubbling up property change...");
                OnPropertyChanged(nameof(SubFolders));
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Path
        {
            get => _path;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(Path)} has changed...");
                    if (_path == value)
                    {
                        logger.Trace($"Value of {nameof(Path)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(Path)} to ""{value}""...");
                    _path = value;

                    logger.Trace("Updating related details...");
                    _directoryInfo = new DirectoryInfo(value);

                    OnPropertyChanged(nameof(Caption));
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<FolderViewModel> SubFolders { get; set; }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}