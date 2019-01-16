using System;
using System.IO;

namespace PhotoLabel.Services
{
    public class ImageMetadataService : IImageMetadataService
    {
        #region private properties
        private readonly ILogService _logService;
        private readonly IXmlFileSerialiser _xmlFileSerialiser;
        #endregion

        public ImageMetadataService(
            ILogService logService,
            IXmlFileSerialiser xmlFileSerialiser)
        {
            // save dependency injections
            _logService = logService;
            _xmlFileSerialiser = xmlFileSerialiser;
        }

        public bool Delete(string filename)
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
                if (!File.Exists(metadataFilename))
                {
                    _logService.Trace($"File \"{metadataFilename}\" does not exist.  Exiting...");
                    return false;
                }

                _logService.Trace($"Deleting \"{metadataFilename}\"...");
                File.Delete(metadataFilename);

                return true;
            }
            finally
            {
                _logService.TraceExit();
            }
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

        public Models.Metadata Load(string filename)
        {
            _logService.TraceEnter();
            try
            {
                // get the name of the metadata file
                _logService.Trace($"Getting metadata filename for image \"{filename}\"...");
                var metadataFilename = GetMetadataFilename(filename);

                _logService.Trace($"Loading from file \"{metadataFilename}\"...");
                var metadata = _xmlFileSerialiser.Deserialise<Models.Metadata>(metadataFilename);

                // return the metadata
                return metadata;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Save(Models.Metadata metadata, string filename)
        {
            _logService.TraceEnter();
            try {
                // validate that all of the properties have a value
                if (metadata.AppendDateTakenToCaption == null) throw new InvalidOperationException(@"""AppendDateTakenToCaption"" property value not specified");
                if (metadata.BackgroundColour == null) throw new InvalidOperationException(@"""BackgroundColour"" property value not specified");
                if (metadata.CaptionAlignment == null) throw new InvalidOperationException(@"""CaptionAlignment"" property value not specified");
                if (metadata.Colour == null) throw new InvalidOperationException(@"""Colour"" property value not specified");
                if (metadata.FontBold == null) throw new InvalidOperationException(@"""FontBold"" property value not specified");
                if (metadata.FontSize == null)
                    throw new InvalidOperationException(@"""FontSize"" property value not specified");
                if (metadata.ImageFormat == null)
                    throw new InvalidOperationException(@"""ImageFormat"" property value not specified");
                if (metadata.Rotation == null) throw new InvalidOperationException(@"""Rotation"" property value not specified");

                _logService.Trace($"Saving metadata for \"{filename}\"...");

                // get the name of the metadata file
                var metadataFilename = GetMetadataFilename(filename);

                _logService.Trace($@"Saving metadata to ""{metadataFilename}""...");
                _xmlFileSerialiser.Serialise(metadata, metadataFilename);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}