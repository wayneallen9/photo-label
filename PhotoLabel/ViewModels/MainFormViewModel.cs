using Ninject.Parameters;
using PhotoLabel.Services;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
namespace PhotoLabel.ViewModels
{
    public class MainFormViewModel : INotifyPropertyChanged
    {
        #region events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region variables
        private CaptionAlignments _captionAlignment;
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private readonly ILogService _logService;
        private string _outputPath;
        private int _zoom;
        #endregion

        public MainFormViewModel(
            IImageMetadataService imageMetadataService,
            IImageService imageService,
            ILogService logService)
        {
            // save the dependency injections
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
            _logService = logService;

            // initialise the properties from the application settings
            _captionAlignment = Properties.Settings.Default.CaptionAlignment;
            _outputPath = Properties.Settings.Default.OutputPath;
            _zoom = Properties.Settings.Default.Zoom > 0 ? Properties.Settings.Default.Zoom : 100;
        }

        public CaptionAlignments CaptionAlignment
        {
            get => _captionAlignment;
            set
            {
                // set the value
                _captionAlignment = value;

                // persist the value
                Properties.Settings.Default.CaptionAlignment = value;
                Properties.Settings.Default.Save();

                OnPropertyChanged();
            }
        }

        public Color Color
        {
            get
            {
                try
                {
                    return Properties.Settings.Default.Color;
                }
                catch (NullReferenceException)
                {
                    return Color.White;
                }
            }
            set
            {
                // save the color
                Properties.Settings.Default.Color = value;

                // persist the changes
                Properties.Settings.Default.Save();
            }
        }

        public Font Font {
            get => Properties.Settings.Default.Font ?? SystemFonts.DefaultFont;
            set {
                // save the new font
                Properties.Settings.Default.Font = value;

                // persit the change
                Properties.Settings.Default.Save();
            }
        }

        public BindingList<ImageViewModel> Images { get; set; }

        private void OnPropertyChanged([CallerMemberName] string propertyName="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int Open(string folderPath)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Loading image files from \"{folderPath}\" and it's subfolders...");
                var images = Directory.EnumerateFiles(folderPath, "*.*")
                    .Where(s => 
                        (
                            s.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.gif", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.bmp", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.png", StringComparison.CurrentCultureIgnoreCase)
                        ) &&
                        (File.GetAttributes(s) & FileAttributes.Hidden) == 0)
                    .Select(s =>
                    {
                        // get the image
                        var imageViewModel = NinjectKernel.Get<ImageViewModel>();

                        // set the filename
                        imageViewModel.Filename = s;

                        return imageViewModel;
                    }).ToList();

                Images = new BindingList<ImageViewModel>(images);

                return Images.Count;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string OutputPath
        {
            get => _outputPath;
            set
            {
                // save the new value
                _outputPath = value;

                // persist the new value
                Properties.Settings.Default.OutputPath = value;
                Properties.Settings.Default.Save();

                OnPropertyChanged();
            }
        }

        public FormWindowState WindowState {
            get => Properties.Settings.Default.WindowState;
            set {
                // update the settings
                Properties.Settings.Default.WindowState = value;

                // persist the change
                Properties.Settings.Default.Save();
            }
        }

        public int Zoom {
            get => _zoom;
            set {
                // save the new value
                _zoom = value;

                // persist the new value
                Properties.Settings.Default.Zoom = value;
                Properties.Settings.Default.Save();

                OnPropertyChanged();
            }
        }
    }
}