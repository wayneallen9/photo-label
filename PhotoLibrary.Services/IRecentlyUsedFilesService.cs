using System.Collections.Generic;

namespace PhotoLibrary.Services
{
    public interface IRecentlyUsedFilesService
    {
        string GetCaption(string filename);
        List<string> Filenames { get; }

        void Open(string filename);
        void Remove(string filename);
    }
}