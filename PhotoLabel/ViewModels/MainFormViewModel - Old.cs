using PhotoLabel.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace PhotoLabel.ViewModels
{
    public class MainFormViewModel : IObservable<MainFormViewModel>, IObserver<ImageViewModel>, IObserver<IImage>
    {
        #region events
        #endregion

        #region variables
        private CaptionAlignments _defaultCaptionAlignment;
        private int _count;
        private Color _defaultColour;
        private Font _defaultFont;
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IList<ImageViewModel> _images;
        private readonly IImageService _imageService;
        private IDisposable _imageUnsubscriber;
        private readonly ILogService _logService;
        private readonly IList<IObserver<MainFormViewModel>> _observers;
        private string _outputPath;
        private int _position;
        private Color? _secondColour;
        private IDisposable _unsubscriber;
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

            // initalise the variables
            _observers = new List<IObserver<MainFormViewModel>>();
            _position = -1;

            // set the default colour to use for photos
            try
            {
                _defaultColour = Properties.Settings.Default.Color;
            }
            catch (NullReferenceException)
            {
                _defaultColour = Color.White;
            }

            // initialise the properties from the application settings
            _defaultCaptionAlignment = Properties.Settings.Default.CaptionAlignment;
            _outputPath = Properties.Settings.Default.OutputPath;
            _zoom = Properties.Settings.Default.Zoom > 0 ? Properties.Settings.Default.Zoom : 100;
        }

        public string Caption
        {
            get => Current?.Caption ?? string.Empty;
            set
            {
                // only process changes
                if (Caption == value) return;

                // is there a current image?
                if (Current == null) return;

                // save the caption on the current image
                Current.Caption = value;
            }
        }

        public CaptionAlignments CaptionAlignment
        {
            get => Current?.CaptionAlignment ?? DefaultCaptionAlignment;
            set
            {
                // only process changes
                if (CaptionAlignment == value) return;

                // save the new value
                _defaultCaptionAlignment = value;

                // if there is a current image, change it's alignment
                if (Current != null)
                {
                    // set the new alignment
                    Current.CaptionAlignment = value;

                    // reload the image
                    Current.LoadImage(DefaultCaptionAlignment, DefaultColour, DefaultFont);
                }

                Notify();
            }
        }

        public Color Colour
        {
            get => Current?.Colour ?? DefaultColour;
            set
            {
                // only process changes
                if (Colour == value) return;

                // save the current colour as the secondary colour
                _secondColour = Colour;

                // set the new default colour
                _defaultColour = value;

                // is there a current image?
                if (Current != null)
                {
                    // update the colour of the current image
                    Current.Colour = value;

                    // reload the current image
                    Current.LoadImage(DefaultCaptionAlignment, DefaultColour, DefaultFont);
                }

                Notify();
            }
        }

        public int Count
        {
            get => _count;
            set
            {
                // only process changes
                if (_count == value) return;

                // save the new value
                _count = value;

                Notify();
            }
        }

        private ImageViewModel Current => Position > -1 && Position < Images.Count ? Images[Position] : null;

        public CaptionAlignments DefaultCaptionAlignment
        {
            get => _defaultCaptionAlignment;
            set
            {
                // only process changes
                if (_defaultCaptionAlignment == value) return;

                // set the value
                _defaultCaptionAlignment = value;

                // persist the value
                Properties.Settings.Default.CaptionAlignment = value;
                Properties.Settings.Default.Save();

                Notify();
            }
        }

        public Color DefaultColour
        {
            get => _defaultColour;
            set
            {
                // only process changes
                if (_defaultColour == value) return;

                // save the color
                Properties.Settings.Default.Color = value;
                Properties.Settings.Default.Save();

                Notify();
            }
        }

        public Font DefaultFont
        {
            get => _defaultFont ?? SystemFonts.DefaultFont;
            set
            {
                // only process changes
                if (_defaultFont == value) return;

                // update the value
                _defaultFont = value;

                // save the new font
                Properties.Settings.Default.Font = value;
                Properties.Settings.Default.Save();

                Notify();
            }
        }

        private void EnterImage()
        {
            _logService.TraceEnter();
            try
            {
                // subscribe to the new image
                _imageUnsubscriber = Current?.Subscribe(this as IObserver<IImage>);
                _unsubscriber = Current?.Subscribe(this as IObserver<ImageViewModel>);

                // load the new image
                Current?.LoadImage(DefaultCaptionAlignment, DefaultColour, DefaultFont);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Font Font
        {
            get => Current?.Font ?? DefaultFont;
            set
            {
                // has the value changed?
                if (Font == value) return;

                // save the new default font
                _defaultFont = value;

                // is there a current image?
                if (Current != null)
                {
                    // update the font on the current image
                    Current.Font = value;

                    // reload the current image
                    Current.LoadImage(DefaultCaptionAlignment, DefaultColour, DefaultFont);
                }

                Notify();
            }
        }

        public Image Image => Current?.Image;

        private IList<ImageViewModel> Images => _images;

        private void Notify()
        {
            _logService.TraceEnter();
            try
            {
                foreach (var observer in _observers)
                    observer.OnUpdate(this);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void NotifyError(Exception error)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Notifying {_observers.Count} observer(s) of error...");
                foreach (var observer in _observers)
                    observer.OnError(error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public IList<string> Open(string folderPath)
        {
            _logService.TraceEnter();
            try
            {
                // leave the current image
                LeaveImage(-1);

                _logService.Trace($"Retrieving image filenames from \"{folderPath}\" and it's subfolders...");
                var filenames = Directory.EnumerateFiles(folderPath, "*.*")
                    .Where(s =>
                        (
                            s.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.gif", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.bmp", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.png", StringComparison.CurrentCultureIgnoreCase)
                        ) &&
                        (File.GetAttributes(s) & FileAttributes.Hidden) == 0)
                    .ToList();

                _logService.Trace($"Creating {filenames.Count} images...");
                _images.Clear();
                _images.AddRange(
                    filenames.Select(s =>
                    {
                        // get the image
                        var imageViewModel = NinjectKernel.Get<ImageViewModel>();

                        // set the filename
                        imageViewModel.Filename = s;

                        return imageViewModel;
                    })
                );

                // update the count
                _count = filenames.Count();

                // reset the position
                _position = filenames.Count() > 0 ? 0 : -1;

                // enter the first image
                EnterImage();

                Notify();

                return filenames;
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

                Notify();
            }
        }

        public int Position
        {
            get => _position;
            set
            {
                // only process changes
                if (_position == value) return;

                // leave the currently active image
                LeaveImage(value);

                // update the position
                _position = value;

                // enter the new active image
                EnterImage();

                Notify();
            }
        }

        private void LeaveImage(int newPosition)
        {
            _logService.TraceEnter();
            try
            {
                // release the memory for the current images
                Current?.CancelLoadImage();
                Next?.CancelLoadImage();

                // unsubscribe from the last image
                _imageUnsubscriber?.Dispose();
                _unsubscriber?.Dispose();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Color? SecondColour
        {
            get => _secondColour;
            set
            {
                // only process changes
                if (_secondColour == value) return;

                // save the new colour
                _secondColour = value;

                Notify();
            }
        }

        public IDisposable Subscribe(IObserver<MainFormViewModel> observer)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if observer is already observing...");
                if (!_observers.Contains(observer))
                {
                    _logService.Trace("Observer is not already observing.  Adding observer...");
                    _observers.Add(observer);

                    // update with the initial values
                    observer.OnImage(this);
                    observer.OnUpdate(this);
                }

                return new Unsubscriber<MainFormViewModel>(_observers, observer);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private ImageViewModel Next
        {
            get
            {
                // is there a next image?
                if (_position < 0 || _position + 1 >= _images.Count) return null;

                return _images[_position + 1];
            }
        }

        public void OnNext(ImageViewModel value)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if default caption alignment is different to current image caption alignment...");
                if (value.CaptionAlignment != null && DefaultCaptionAlignment != value.CaptionAlignment)
                {
                    _logService.Trace("Default caption alignment is different.  Updating...");
                    _defaultCaptionAlignment = value.CaptionAlignment.Value;
                }

                _logService.Trace("Checking if default colour is different from current image colour...");
                if (value.Colour != null && DefaultColour != value.Colour)
                {
                    _logService.Trace("Default colour is different.  Updating...");
                    _secondColour = _defaultColour;
                    _defaultColour = value.Colour.Value;
                }

                _logService.Trace("Checking if default font is different from current image font...");
                if (value.Font != null && DefaultFont != value.Font)
                {
                    _logService.Trace("Default font is different.  Updating...");
                    _defaultFont = value.Font;
                }

                _logService.Trace($"Reloading image \"{value.Filename}\"...");
                value.LoadImage(DefaultCaptionAlignment, DefaultColour, DefaultFont);

                _logService.Trace($"Bubbling up change to \"{value.Filename}\"...");
                Notify();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void OnError(Exception error)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Bubbling up error...");
                NotifyError(error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public FormWindowState WindowState
        {
            get => Properties.Settings.Default.WindowState;
            set
            {
                // only process changes
                if (Properties.Settings.Default.WindowState == value) return;

                // update the settings
                Properties.Settings.Default.WindowState = value;
                Properties.Settings.Default.Save();

                Notify();
            }
        }

        public int Zoom
        {
            get => _zoom;
            set
            {
                // only process changes
                if (_zoom == value) return;

                // save the new value
                _zoom = value;

                // persist the new value
                Properties.Settings.Default.Zoom = value;
                Properties.Settings.Default.Save();

                Notify();
            }
        }
    }
}