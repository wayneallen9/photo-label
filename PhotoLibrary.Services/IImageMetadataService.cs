using PhotoLabel.Services.Models;
namespace PhotoLabel.Services
{
    public interface IImageMetadataService
    {
        Metadata Load(string filename);
        void Save(Metadata metadata, string filename);
    }
}