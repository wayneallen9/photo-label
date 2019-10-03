using System;
using System.IO;
using Shared;
using Shared.Attributes;

namespace PhotoLabel.Services
{
    [Singleton]
    public class ImageMetadataService : IImageMetadataService
    {
        #region private properties
        private readonly ILogger _logger;
        private readonly IXmlFileSerialiser _xmlFileSerialiser;
        #endregion

        public ImageMetadataService(
            ILogger logger,
            IXmlFileSerialiser xmlFileSerialiser)
        {
            // save dependency injections
            _logger = logger;
            _xmlFileSerialiser = xmlFileSerialiser;
        }

        public bool Delete(string filename)
        {
            using (var logger = _logger.Block())
            {
                // get the name of the metadata file
                logger.Trace($"Getting metadata filename for image \"{filename}\"...");
                var metadataFilename = GetMetadataFilename(filename);
                logger.Trace($"Metadata filename is \"{metadataFilename}\"");

                // does the metadata exist?
                logger.Trace($"Checking if file \"{metadataFilename}\" exists...");
                if (!File.Exists(metadataFilename))
                {
                    logger.Trace($"File \"{metadataFilename}\" does not exist.  Exiting...");
                    return false;
                }

                logger.Trace($"Deleting \"{metadataFilename}\"...");
                File.Delete(metadataFilename);

                return true;

            }
        }

        private string GetMetadataFilename(string filename)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($"Getting metadata filename for \"{filename}\"...");
                var metadataFilename = $"{filename}.xml";
                logger.Trace($"Metadata filename for \"{filename}\" is \"{metadataFilename}\"");

                return metadataFilename;

            }
        }

        public Models.Metadata Load(string filename)
        {
            using (var logger = _logger.Block())
            {
                // get the name of the metadata file
                logger.Trace($"Getting metadata filename for image \"{filename}\"...");
                var metadataFilename = GetMetadataFilename(filename);

                logger.Trace($"Loading from file \"{metadataFilename}\"...");
                var metadata = _xmlFileSerialiser.Deserialise<Models.Metadata>(metadataFilename);

                // return the metadata
                return metadata;

            }
        }

        public Models.Metadata Populate(Models.Metadata metadata)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Populating any incomplete metadata values with defaults...");
                metadata.AppendDateTakenToCaption = metadata.AppendDateTakenToCaption ?? false;
                metadata.BackgroundColour = metadata.BackgroundColour ?? System.Drawing.Color.Transparent.ToArgb();
                metadata.CaptionAlignment = metadata.CaptionAlignment ?? CaptionAlignments.BottomRight;
                metadata.Colour = metadata.Colour ?? System.Drawing.Color.White.ToArgb();
                metadata.FontBold = metadata.FontBold ?? false;
                metadata.FontFamily = metadata.FontFamily ?? System.Drawing.SystemFonts.DefaultFont.SystemFontName;
                metadata.FontSize = metadata.FontSize ?? 10;
                metadata.FontType = metadata.FontType ?? "pts";
                metadata.ImageFormat = metadata.ImageFormat ?? ImageFormat.Jpeg;
                metadata.Rotation = metadata.Rotation ?? Rotations.Zero;

                return metadata;
            }
        }
        public void Rename(string oldFilename, string newFilename)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Getting metadata filename for ""{oldFilename}""...");
                var oldMetadataFilename = GetMetadataFilename(oldFilename);

                logger.Trace($@"Getting metadata filename for ""{newFilename}""...");
                var newMetadataFilename = GetMetadataFilename(newFilename);

                logger.Trace($@"Renaming ""{oldMetadataFilename}"" to ""{newMetadataFilename}""...");
                File.Move(oldMetadataFilename, newMetadataFilename);
            }
        }

        public void Save(Models.Metadata metadata, string filename)
        {
            using (var logger = _logger.Block())
            {
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

                logger.Trace($"Saving metadata for \"{filename}\"...");

                // get the name of the metadata file
                var metadataFilename = GetMetadataFilename(filename);

                logger.Trace($@"Saving metadata to ""{metadataFilename}""...");
                _xmlFileSerialiser.Serialise(metadata, metadataFilename);

            }
        }

        public bool Exists(string filename)
        {
            using (var logger = _logger.Block())
            {
                // get the name of the metadata file
                logger.Trace($"Getting metadata filename for image \"{filename}\"...");
                var metadataFilename = GetMetadataFilename(filename);
                logger.Trace($"Metadata filename is \"{metadataFilename}\"");

                // does the metadata exist?
                logger.Trace($"Checking if file \"{metadataFilename}\" exists...");
                return File.Exists(metadataFilename);
            }
        }
    }
}