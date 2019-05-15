using System.Collections.Generic;

namespace PhotoLabel.Wpf
{
    public class FolderViewModel
    {
        #region events
        #endregion

        public string Caption { get; set; }
        public string Filename { get; set; }
        public string Path { get; set; }
        public List<SubFolderViewModel> SubFolders { get; set; }
    }
}