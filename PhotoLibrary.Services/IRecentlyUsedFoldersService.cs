using PhotoLabel.Services.Models;
using System.Collections.Generic;

namespace PhotoLabel.Services
{
    public interface IRecentlyUsedFoldersService
    {
        List<DirectoryModel> Load();
        List<DirectoryModel> Add(string folder, List<DirectoryModel> folders);
        List<DirectoryModel> Save(List<DirectoryModel> folders);
    }
}