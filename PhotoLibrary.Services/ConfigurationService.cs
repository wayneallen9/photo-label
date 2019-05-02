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
    public class ConfigurationService : IConfigurationService
    {
        public ConfigurationService(
            ILogService logService,
            IXmlFileSerialiser xmlFileSerialiser)
        {
            // save dependency injections
            _logService = logService;
            _xmlFileSerialiser = xmlFileSerialiser;

            // load the values from file
            _configurationModel = Load();
        }

        public bool AppendDateTakenToCaption
        {
            get => _configurationModel.AppendDateTakenToCaption;
            set
            {
                // update the value
                _configurationModel.AppendDateTakenToCaption = value;

                // save the change
                Save();
            }
        }

        public Color BackgroundColour
        {
            get => _configurationModel.BackgroundColour ?? Colors.Transparent;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Setting new value of {nameof(BackgroundColour)}...");
                    _configurationModel.BackgroundColour = value;

                    _logService.Trace($"Persisting new value of {nameof(BackgroundColour)}...");
                    Save();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public CaptionAlignments CaptionAlignment
        {
            get => _configurationModel.CaptionAlignment;
            set
            {
                // update the value
                _configurationModel.CaptionAlignment = value;

                // save the change
                Save();
            }
        }

        public double CaptionSize
        {
            get => _configurationModel.CaptionSize ?? 12;
            set
            {
                // save the new size
                _configurationModel.CaptionSize = value;

                Save();
            }
        }
        public Color Colour
        {
            get => _configurationModel.Colour ?? new Color();
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
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking new value is valid...");
                    if (value != "%" && value != "pts") throw new ArgumentOutOfRangeException(nameof(FontType));

                    // update the value
                    _configurationModel.FontType = value;

                    // persist the change
                    Save();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public ImageFormat ImageFormat
        {
            get => _configurationModel.ImageFormat;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($@"Setting value of {nameof(ImageFormat)} to {value}...");
                    _configurationModel.ImageFormat = value;

                    _logService.Trace("Saving change...");
                    Save();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public ulong? MaxImageSize
        {
            get => _configurationModel.MaxImageSize;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Setting value of {nameof(MaxImageSize)} to {value}...");
                    _configurationModel.MaxImageSize = value;

                    _logService.Trace("Saving change...");
                    Save();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string OutputPath
        {
            get => _configurationModel.OutputPath;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Saving new value for {nameof(OutputPath)}...");
                    _configurationModel.OutputPath = value;

                    _logService.Trace("Persisting new value...");
                    Save();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public FormWindowState WindowState
        {
            get => _configurationModel.WindowState;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Setting new value of {nameof(WindowState)} to {value}...");
                    _configurationModel.WindowState = value;

                    _logService.Trace($"Persisting new value of {nameof(WindowState)}...");
                    Save();
                }
                finally
                {
                    _logService.TraceExit();
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
            _logService.TraceEnter();
            try
            {
                // build the filename for the recently used files
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Photo Label",
                    "Configuration.xml");
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private Models.Configuration Load()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if configuration has already been loaded...");
                if (_configurationModel != null)
                {
                    _logService.Trace("Configuration has already been loaded.  Returning...");
                    return _configurationModel;
                }

                _logService.Trace("Getting path to configuration file...");
                var filename = GetFilename();

                _logService.Trace($@"Checking if configuration file ""{filename}"" exists...");
                if (!File.Exists(filename))
                {
                    _logService.Trace($@"Configuration file ""{filename}"" does not exist.  Creating configuration...");
                    return CreateConfigurationModel();
                }
                else
                {
                    try
                    {
                        return _xmlFileSerialiser.Deserialise<Models.Configuration>(filename);
                    }
                    catch (Exception ex)
                    {
                        // the configuration could not be loaded, default it
                        _logService.Error(ex);

                        return CreateConfigurationModel();
                    }
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public IList<Color> RecentlyUsedBackColors
        {
            get => _configurationModel.RecentlyUsedBackColors;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Setting new value of {nameof(RecentlyUsedBackColors)}...");
                    _configurationModel.RecentlyUsedBackColors = value.ToList();

                    _logService.Trace($"Persisting new value of {nameof(WindowState)}...");
                    Save();
                }
                finally
                {
                    _logService.TraceExit();
                }

            }
        }

        private void Save()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting path to configuration file...");
                var filename = GetFilename();

                _logService.Trace($@"Getting directory for ""{filename}""...");
                var directory = Path.GetDirectoryName(filename);
                if (directory != null)
                {
                    _logService.Trace($"Ensuring that all parent directories exist for \"{filename}\"...");
                    Directory.CreateDirectory(directory);
                }

                _logService.Trace($"Saving configuration to \"{filename}\"...");
                _xmlFileSerialiser.Serialise(_configurationModel, filename);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string WhereUrl => ConfigurationManager.AppSettings["WhereUrl"];

        #region variables

        private readonly Models.Configuration _configurationModel;
        private readonly ILogService _logService;
        private readonly IXmlFileSerialiser _xmlFileSerialiser;

        #endregion
    }
}