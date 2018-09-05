using System.Drawing;
using PhotoLibrary.Services;
namespace PhotoLibrary.Models
{
    public class ImageViewModel
    {
        #region variables
        private Brush _brush;
        private string _caption;
        private string _filename;
        private Font _font;
        private Image _image;
        private readonly IImageService _imageService;
        private Point _location;
        #endregion

        public ImageViewModel(IImageService imageService)
        {
            // save the injections
            _imageService = imageService;

            // set a default location for the caption
            _location = new Point(10, 10);
        }

        public Brush Brush {
            get => _brush;
            set {
                // save the brush
                _brush = value;

                // invalidate the cache
                _image = null;
            }
        }

        public string Caption {
            get
            {
                // has a caption been set?
                if (_caption != null) return _caption;

                // set the default caption
                _caption = _imageService.GetDateTaken(Filename);

                return _caption;
            }
            set
            {
                // save the new caption
                _caption = value;

                // invalidate the cache
                _image = null;
            }
        }

        public string Filename {
            get => _filename;
            set
            {
                // save the filename
                _filename = value;

                // invalidate the cache
                _image = null;
            }
        }

        public Font Font {
            get => _font;
            set {
                // save the new value
                _font = value;

                // invalidate the cache
                _image = null;
            }
        }

        public Image Image {
            get
            {
                // do we have a cached version of the image?
                if (_image != null) return _image;

                // do we have all of the properties required?
                if (Caption == null || Font == null || Brush == null) return null;

                // get the image
                var image = _imageService.Get(Filename);
                
                // cache the image with the caption
                _image = _imageService.Caption(image, Caption, Font, Brush, Location);

                return _image;
            }
        }

        public Point Location
        {
            get => _location;
            set
            {
                // save the new value
                _location = value;

                // invalidate the cache
                _image = null;
            }
        }
    }
}