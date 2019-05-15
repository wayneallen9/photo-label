using PhotoLabel.Services.Models;
namespace PhotoLabel.Services
{
    public interface IImageMetadataService
    {
        bool Delete(string filename);
        Metadata Load(string filename);
        void Rename(string oldFilename, string newFilename);
        void Save(Metadata metadata, string filename);
    }
}