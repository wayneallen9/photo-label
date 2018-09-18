using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
namespace PhotoLabel.Services
{
    public class ImageMetadataService : IImageMetadataService
    {
        #region private properties
        private string _filename;
        private readonly ILogService _logService;
        private ImageMetadata _imageMetadata;
        #endregion

        public ImageMetadataService(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;
        }

        public bool HasMetadata(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Loading metadata for \"{filename}\"...");

                // load the metadata
                var metadata = LoadMetadata(filename);

                return metadata != null;
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
                var metadataFilename = $"{Path.GetDirectoryName(filename)}\\{Path.GetFileNameWithoutExtension(filename)}.dat";
                _logService.Trace($"Metadata filename for \"{filename}\" is \"{metadataFilename}\"");

                return metadataFilename;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string LoadCaption(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Loading metadata for \"{filename}\"...");

                // load the metadata
                var metadata = LoadMetadata(filename);

                if (metadata != null) return metadata.Caption;
            }
            finally
            {
                _logService.TraceExit();
            }

            return null;
        }

        public CaptionAlignments? LoadCaptionAlignment(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Loading metadata for \"{filename}\"...");

                // load the metadata
                var metadata = LoadMetadata(filename);

                if (metadata != null) return metadata.CaptionAlignment;
            }
            finally
            {
                _logService.TraceExit();
            }

            return null;
        }

        public Color? LoadColor(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Loading metadata for \"{filename}\"...");

                // load the metadata
                var metadata = LoadMetadata(filename);

                if (metadata != null) return metadata.Color;
            }
            finally
            {
                _logService.TraceExit();
            }

            return null;
        }

        public Font LoadFont(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Loading metadata for \"{filename}\"...");

                // load the metadata
                var metadata = LoadMetadata(filename);

                if (metadata != null) return metadata.Font;
            }
            finally
            {
                _logService.TraceExit();
            }

            return null;
        }

        private ImageMetadata LoadMetadata(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if metadata for \"{filename}\" has been cached...");
                if (_filename == filename)
                {
                    _logService.Trace($"Metadata for \"{filename}\" has been cached");
                    return _imageMetadata;
                }

                _logService.Trace($"Metadata for \"{filename}\" has not been cached");

                // get the name of the metadata file
                var metadataFilename = GetMetadataFilename(filename);

                // does the metadata exist?
                if (File.Exists(metadataFilename))
                {
                    var serializer = new BinaryFormatter();
                    using (var fileStream = new FileStream(metadataFilename, FileMode.Open, FileAccess.Read))
                    {
                        try
                        {
                            _imageMetadata = serializer.Deserialize(fileStream) as ImageMetadata;
                        }
                        catch (SerializationException)
                        {
                            // ignore serialization errors
                        }
                    }

                    // save the filename as the cache
                    _filename = filename;
                }
                else
                {
                    _logService.Trace($"Metadata file not found for \"{filename}\"");

                    // save the filename as the cache
                    _filename = filename;
                    _imageMetadata = null;
                }

                // return the metadata
                return _imageMetadata;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Rotations? LoadRotation(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Loading metadata for \"{filename}\"...");

                // load the metadata
                var metadata = LoadMetadata(filename);

                if (metadata != null) return metadata.Rotation;
            }
            finally
            {
                _logService.TraceExit();
            }

            return null;
        }

        public void Save(string caption, CaptionAlignments captionAlignment, Font font, Color color, Rotations rotation, string filename)
        {
            _logService.TraceEnter();
            try {
                _logService.Trace($"Saving metadata for \"{filename}\"...");

                // get the name of the metadata file
                var metadataFilename = GetMetadataFilename(filename);

                // now save it
                var serializer = new BinaryFormatter();
                using (var writer = new FileStream(metadataFilename, FileMode.Create, FileAccess.Write))
                {
                    serializer.Serialize(writer, new ImageMetadata
                    {
                        Caption=caption,
                        CaptionAlignment=captionAlignment,
                        Font=font,
                        Color=color,
                        Rotation=rotation
                    });
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        [Serializable]
        private class ImageMetadata
        {
            public Color Color { get; set; }
            public string Caption { get; set; }
            public CaptionAlignments CaptionAlignment { get; set; }
            public Font Font { get; set; }
            public Rotations Rotation { get; set; }
        }
    }
}
