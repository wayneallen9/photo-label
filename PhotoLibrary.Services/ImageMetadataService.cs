using PhotoLabel.Services.Models;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
namespace PhotoLabel.Services
{
    public class ImageMetadataService : IImageMetadataService
    {
        #region private properties
        private readonly ILogService _logService;
        #endregion

        public ImageMetadataService(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;
        }

        private string GetMetadataFilename(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Getting metadata filename for \"{filename}\"...");
                var metadataFilename = $"{filename}.xml";
                _logService.Trace($"Metadata filename for \"{filename}\" is \"{metadataFilename}\"");

                return metadataFilename;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Metadata Load(string filename)
        {
            _logService.TraceEnter();
            try
            {
                // get the name of the metadata file
                _logService.Trace($"Getting metadata filename for image \"{filename}\"...");
                var metadataFilename = GetMetadataFilename(filename);
                _logService.Trace($"Metadata filename is \"{metadataFilename}\"");

                // does the metadata exist?
                _logService.Trace($"Checking if file \"{metadataFilename}\" exists...");
                if (File.Exists(metadataFilename))
                {
                    _logService.Trace($"File \"{metadataFilename}\" exists.  Deserialising...");
                    var serializer = new XmlSerializer(typeof(Metadata));
                    using (var fileStream = new FileStream(metadataFilename, FileMode.Open, FileAccess.Read))
                    {
                        try
                        {
                            return serializer.Deserialize(fileStream) as Metadata;
                        }
                        catch (SerializationException)
                        {
                            // ignore serialization errors
                            _logService.Trace($"Unable to deserialise \"{metadataFilename}\".  Ignoring.");
                        }
                    }
                }
                else
                {
                    _logService.Trace($"Metadata file not found for \"{filename}\"");
                }

                // return the metadata
                return null;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Save(Metadata metadata, string filename)
        {
            _logService.TraceEnter();
            try {
                _logService.Trace($"Saving metadata for \"{filename}\"...");

                // get the name of the metadata file
                var metadataFilename = GetMetadataFilename(filename);

                // now save it
                var serializer = new XmlSerializer(metadata.GetType());
                using (var writer = new FileStream(metadataFilename, FileMode.Create, FileAccess.Write))
                {
                    serializer.Serialize(writer, metadata);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}