using System;
using System.IO;
using System.Xml.Serialization;
using Shared;
using Shared.Attributes;

namespace PhotoLabel.Services
{
    [Singleton]
    public class XmlFileSerialiser : IXmlFileSerialiser
    {
        #region variables
        private readonly ILogger _logger;
        #endregion

        public XmlFileSerialiser(
            ILogger logger)
        {
            // save dependency injections
            _logger = logger;
        }

        public T Deserialise<T>(string path) where T : class 
        {
            using (var logger = _logger.Block()) {
                try
                {
                    logger.Trace($@"Checking if ""{path}"" exists...");
                    if (!File.Exists(path))
                    {
                        logger.Trace($@"""{path}"" does not exist.  Returning...");
                        return null;
                    }

                    logger.Trace($@"Opening ""{path}""...");
                    using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        logger.Trace($@"Deserialising from ""{path}""...");
                        var serialiser = new XmlSerializer(typeof(T));
                        return serialiser.Deserialize(fileStream) as T;
                    }
                }
                catch (InvalidOperationException)
                {
                    return null;

                }
            }
        }

        public void Serialise(object o, string path)
        {
            using (var logger = _logger.Block()) {
                logger.Trace($@"Ensuring that directory exists for ""{path}""...");
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());

                logger.Trace($@"Opening ""{path}""...");
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    logger.Trace($@"Serialising to ""{path}""...");
                    var serialiser = new XmlSerializer(o.GetType());
                    serialiser.Serialize(fileStream, o);
                }
            
            }
        }
    }
}