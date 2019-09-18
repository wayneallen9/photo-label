using Shared;
using Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace PhotoLabel.Services
{
    [Singleton]
    public class ConfigurationService : IConfigurationService
    {
        public ConfigurationService(
            ILogger logger,
            IXmlFileSerialiser xmlFileSerialiser)
        {
            // save dependency injections
            _logger = logger;
            _xmlFileSerialiser = xmlFileSerialiser;

            // load the values from file
            _configurationModel = Load();
        }

        public bool AppendDateTakenToCaption
        {
            get => _configurationModel.AppendDateTakenToCaption;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(AppendDateTakenToCaption)} has changed...");
                    if (_configurationModel.AppendDateTakenToCaption == value)
                    {
                        logger.Trace($"Value of {nameof(AppendDateTakenToCaption)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting new value of {nameof(AppendDateTakenToCaption)}...");
                    _configurationModel.AppendDateTakenToCaption = value;

                    logger.Trace($"Saving new value of {nameof(AppendDateTakenToCaption)}...");
                    Save();
                }
            }
        }

        public Color BackgroundColour
        {
            get => _configurationModel.BackgroundColour ?? Colors.Transparent;
            set
            {
                using (var logger = _logger.Block()) {
                    logger.Trace($"Checking if value of {nameof(BackgroundColour)} has changed...");
                    if (_configurationModel.BackgroundColour == value)
                    {
                        logger.Trace($"Value of {nameof(BackgroundColour)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting new value of {nameof(BackgroundColour)}...");
                    _configurationModel.BackgroundColour = value;

                    logger.Trace($"Persisting new value of {nameof(BackgroundColour)}...");
                    Save();
                }
            }
        }

        public int CanvasHeight
        {
            get => _configurationModel.CanvasHeight ?? 600;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(CanvasHeight)} has changed...");
                    if (_configurationModel.CanvasHeight == value)
                    {
                        logger.Trace($"Value of {nameof(CanvasHeight)} has not changed.  Exiting...");
                        return;
                    }

                    // update the value
                    _configurationModel.CanvasHeight = value;

                    // save the change
                    Save();
                }
            }
        }

        public int CanvasWidth
        {
            get => _configurationModel.CanvasWidth ?? 800;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(CanvasWidth)} has changed...");
                    if (_configurationModel.CanvasWidth == value)
                    {
                        logger.Trace($"Value of {nameof(CanvasWidth)} has not changed.  Exiting...");
                        return;
                    }

                    // update the value
                    _configurationModel.CanvasWidth = value;

                    // save the change
                    Save();
                }
            }
        }

        public CaptionAlignments CaptionAlignment
        {
            get => _configurationModel.CaptionAlignment;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(CaptionAlignment)} has changed...");
                    if (_configurationModel.CaptionAlignment == value)
                    {
                        logger.Trace($"Value of {nameof(CaptionAlignment)} has not changed.  Exiting...");
                        return;
                    }

                    // update the value
                    _configurationModel.CaptionAlignment = value;

                    // save the change
                    Save();
                }
            }
        }

        public double CaptionSize
        {
            get => _configurationModel.CaptionSize ?? 12;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(CaptionSize)} has changed...");
                    if (Math.Abs((_configurationModel.CaptionSize ?? 12) - value) < double.Epsilon)
                    {
                        logger.Trace($"Value of {nameof(CaptionSize)} has not changed.  Exiting...");
                        return;
                    }

                    // save the new size
                    _configurationModel.CaptionSize = value;

                    Save();
                }
            }
        }

        public Color Colour
        {
            get => _configurationModel.Colour ?? Colors.White;
            set
            {
                // save the new value
                _configurationModel.Colour = value;

                // persist the change
                Save();
            }
        }

        public bool FontBold
        {
            get => _configurationModel.FontBold;
            set
            {
                // update the value
                _configurationModel.FontBold = value;

                // save the change
                Save();
            }
        }

        public string FontName
        {
            get => _configurationModel.FontName;
            set
            {
                // update the value
                _configurationModel.FontName = value;

                // save the change
                Save();
            }
        }

        public float FontSize
        {
            get => _configurationModel.FontSize;
            set
            {
                // update the value
                _configurationModel.FontSize = value;

                // save the change
                Save();
            }
        }

        public string FontType
        {
            get => _configurationModel.FontType;
            set
            {
                using (var logger = _logger.Block()) {
                    logger.Trace("Checking new value is valid...");
                    if (value != "%" && value != "pts") throw new ArgumentOutOfRangeException(nameof(FontType));

                    // update the value
                    _configurationModel.FontType = value;

                    // persist the change
                    Save();
                }
            }
        }

        public ImageFormat ImageFormat
        {
            get => _configurationModel.ImageFormat;
            set
            {
                using (var logger = _logger.Block()) {
                    logger.Trace($@"Setting value of {nameof(ImageFormat)} to {value}...");
                    _configurationModel.ImageFormat = value;

                    logger.Trace("Saving change...");
                    Save();
                }
            }
        }

        public ulong? MaxImageSize
        {
            get => _configurationModel.MaxImageSize;
            set
            {
                using (var logger = _logger.Block()) {
                    logger.Trace($"Setting value of {nameof(MaxImageSize)} to {value}...");
                    _configurationModel.MaxImageSize = value;

                    logger.Trace("Saving change...");
                    Save();
                }
            }
        }

        public string OutputPath
        {
            get => _configurationModel.OutputPath;
            set
            {
                using (var logger = _logger.Block()) {
                    logger.Trace($"Saving new value for {nameof(OutputPath)}...");
                    _configurationModel.OutputPath = value;

                    logger.Trace("Persisting new value...");
                    Save();
                
                }
            }
        }

        public FormWindowState WindowState
        {
            get => _configurationModel.WindowState;
            set
            {
                using (var logger = _logger.Block()) {
                    logger.Trace($"Setting new value of {nameof(WindowState)} to {value}...");
                    _configurationModel.WindowState = value;

                    logger.Trace($"Persisting new value of {nameof(WindowState)}...");
                    Save();
                
                }
            }
        }

        private static Models.Configuration CreateConfigurationModel()
        {
            return new Models.Configuration
            {
                FontName = SystemFonts.DefaultFont.Name,
                FontSize = 10f,
                FontType = "%",
                RecentlyUsedBackColors = new List<Color>()
            };
        }

        private string GetFilename()
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Building path to configuration file...");
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Photo Label",
                    "Configuration.xml");
            
            }
        }

        private Models.Configuration Load()
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Checking if configuration has already been loaded...");
                if (_configurationModel != null)
                {
                    logger.Trace("Configuration has already been loaded.  Returning...");
                    return _configurationModel;
                }

                logger.Trace("Getting path to configuration file...");
                var filename = GetFilename();

                logger.Trace($@"Checking if configuration file ""{filename}"" exists...");
                if (!File.Exists(filename))
                {
                    logger.Trace($@"Configuration file ""{filename}"" does not exist.  Creating configuration...");
                    return CreateConfigurationModel();
                }
                else
                {
                    try { 
                        return _xmlFileSerialiser.Deserialise<Models.Configuration>(filename);
                    }
                    catch (Exception ex)
                    {
                        // the configuration could not be loaded, default it
                        logger.Error(ex);

                        return CreateConfigurationModel();
                    }
                }
            
            }
        }

        public IList<Color> RecentlyUsedBackColors
        {
            get => _configurationModel.RecentlyUsedBackColors;
            set
            {
                using (var logger = _logger.Block()) {
                    logger.Trace($"Setting new value of {nameof(RecentlyUsedBackColors)}...");
                    _configurationModel.RecentlyUsedBackColors = value.ToList();

                    logger.Trace($"Persisting new value of {nameof(WindowState)}...");
                    Save();
                
                }

            }
        }

        private void Save()
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Getting path to configuration file...");
                var filename = GetFilename();

                logger.Trace($@"Getting directory for ""{filename}""...");
                var directory = Path.GetDirectoryName(filename);
                if (directory != null)
                {
                    logger.Trace($"Ensuring that all parent directories exist for \"{filename}\"...");
                    Directory.CreateDirectory(directory);
                }

                logger.Trace($"Saving configuration to \"{filename}\"...");
                _xmlFileSerialiser.Serialise(_configurationModel, filename);
            
            }
        }

        public bool UseCanvas
        {
            get => _configurationModel.UseCanvas;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(UseCanvas)} has changed...");
                    if (_configurationModel.UseCanvas == value)
                    {
                        logger.Trace($"Value of {nameof(UseCanvas)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting new value of {nameof(UseCanvas)}...");
                    _configurationModel.UseCanvas = value;

                    logger.Trace($"Persisting new value of {nameof(UseCanvas)}...");
                    Save();
                }
            }
        }

        public string WhereUrl => ConfigurationManager.AppSettings["WhereUrl"];

        #region variables

        private readonly Models.Configuration _configurationModel;
        private readonly ILogger _logger;
        private readonly IXmlFileSerialiser _xmlFileSerialiser;

        #endregion
    }
}