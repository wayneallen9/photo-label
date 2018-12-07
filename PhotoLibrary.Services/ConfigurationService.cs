using PhotoLabel.Services.Models;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
namespace PhotoLabel.Services
{
    public class ConfigurationService : IConfigurationService
    {
        #region variables
        private readonly ConfigurationModel _configurationModel;
        private readonly ILogService _logService;
        #endregion

        public ConfigurationService(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;

            // load the values from file
            _configurationModel = Load();
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

        public Color Colour
        {
            get => Color.FromArgb(_configurationModel.Colour);
            set
            {
                // save the new value
                _configurationModel.Colour = value.ToArgb();

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
                // update the value
                _configurationModel.FontType = value;

                // persist the change
                Save();
            }
        }

        private ConfigurationModel Load()
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
                string filename = GetFilename();

                _logService.Trace($@"Checking if configuration file ""{filename}"" exists...");
                if (!File.Exists(filename))
                {
                    _logService.Trace($@"Configuration file ""{filename}"" does not exist.  Creating configuration...");
                    return new ConfigurationModel
                    {
                        Colour = Color.White.ToArgb(),
                        FontName = SystemFonts.DefaultFont.Name,
                        FontSize = SystemFonts.DefaultFont.SizeInPoints,
                        FontType = "pts"
                    };
                }
                else
                {
                    try
                    {
                        using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                        {
                            // create the deserialiser
                            var serialise = new XmlSerializer(typeof(ConfigurationModel));

                            // deserialise the file
                            return serialise.Deserialize(fileStream) as ConfigurationModel;
                        }
                    }
                    catch (Exception ex)
                    {
                        // the configuration could not be loaded, default it
                        _logService.Error(ex);

                        return new ConfigurationModel
                        {
                            FontName = SystemFonts.DefaultFont.Name,
                            FontSize = SystemFonts.DefaultFont.SizeInPoints,
                            FontType = "pts"
                        };
                    }
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public bool LoadLastFolder
        {
            get => _configurationModel.LoadLastFolder;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(LoadLastFolder)} has changed...");
                    if (_configurationModel.LoadLastFolder == value)
                    {
                        _logService.Trace($"Value of {nameof(LoadLastFolder)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(LoadLastFolder)} to {value}...");
                    _configurationModel.LoadLastFolder = value;

                    _logService.Trace("Persisting change...");
                    Save();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private string GetFilename()
        {
            _logService.TraceEnter();
            try
            {
                // build the filename for the recently used files
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Photo Label", "Configuration.xml");
            }
            finally
            {
                _logService.TraceExit();
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
                    // only process changes
                    if (_configurationModel.OutputPath == value) return;

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
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    var serialiser = new XmlSerializer(_configurationModel.GetType());

                    serialiser.Serialize(fileStream, _configurationModel);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Color? SecondColour
        {
            get
            {
                if (_configurationModel.SecondColour == null) return null;

                return Color.FromArgb(_configurationModel.SecondColour.Value);
            }
            set
            {
                // update the value
                _configurationModel.SecondColour = value?.ToArgb();

                // persist it
                Save();
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
                    _logService.Trace($"Checking if value of {nameof(WindowState)} has changed...");
                    if (_configurationModel.WindowState == value)
                    {
                        _logService.Trace($"Value of {nameof(WindowState)} has not changed.  Exiting...");
                        return;
                    }

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
    }
}