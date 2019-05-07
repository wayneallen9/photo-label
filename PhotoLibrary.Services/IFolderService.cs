using PhotoLabel.Services.Models;

namespace PhotoLabel.Services
{
    public interface IFolderService
    {
        Folder Open(string path);
    }
}