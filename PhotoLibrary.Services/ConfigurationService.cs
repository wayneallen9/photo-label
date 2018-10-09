using PhotoLabel.Services.Models;
using System;
using System.Drawing;
using System.IO;
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

                _logService.Trace($"Checking if configuration file \"{filename}\" exists...");
                if (!File.Exists(filename))
                {
                    _logService.Trace($"Configuration file \"{filename}\" does not exist.  Creating configuration...");
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
                            // create the deserializer
                            var serializer = new XmlSerializer(typeof(ConfigurationModel));

                            // deserialize the file
                            return serializer.Deserialize(fileStream) as ConfigurationModel;
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

        private void Save()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting path to configuration file...");
                string filename = GetFilename();

                _logService.Trace($"Ensuring that all parent directories exist for \"{filename}\"...");
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

                _logService.Trace($"Saving configuration to \"{filename}\"...");
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new XmlSerializer(_configurationModel.GetType());

                    serializer.Serialize(fileStream, _configurationModel);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}