using System.IO;
using System.Xml.Serialization;

namespace PhotoLabel.Services
{
    public class XmlFileSerialiser : IXmlFileSerialiser
    {
        #region variables
        private readonly ILogService _logService;
        #endregion

        public XmlFileSerialiser(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;
        }

        public T Deserialise<T>(string path) where T : class 
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Opening ""{path}""...");
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    _logService.Trace($@"Deserialising from ""{path}""...");
                    var serialiser = new XmlSerializer(typeof(T));
                    return serialiser.Deserialize(fileStream) as T;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Serialise(object o, string path)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Opening ""{path}""...");
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    _logService.Trace($@"Serialising to ""{path}""...");
                    var serialiser = new XmlSerializer(o.GetType());
                    serialiser.Serialize(fileStream, o);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}