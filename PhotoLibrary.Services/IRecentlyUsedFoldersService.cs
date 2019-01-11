using PhotoLabel.Services.Models;
using System.Collections.Generic;

namespace PhotoLabel.Services
{
    public interface IRecentlyUsedFoldersService
    {
        List<Directory> Load();
        List<Directory> Add(string folder, List<Directory> folders);
        List<Directory> Save(List<Directory> folders);
    }
}