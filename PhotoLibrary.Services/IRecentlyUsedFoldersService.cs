using PhotoLabel.Services.Models;
using System.Collections.Generic;

namespace PhotoLabel.Services
{
    public interface IRecentlyUsedFoldersService
    {
        List<FolderModel> Load();
        List<FolderModel> Add(string folder, List<FolderModel> folders);
        List<FolderModel> Save(List<FolderModel> folders);
    }
}