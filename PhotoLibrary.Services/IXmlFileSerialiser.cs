namespace PhotoLabel.Services
{
    public interface IXmlFileSerialiser
    {
        T Deserialise<T>(string path) where T : class;
        void Serialise(object o, string path);
    }
}