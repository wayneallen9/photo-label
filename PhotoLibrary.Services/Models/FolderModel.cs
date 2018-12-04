namespace PhotoLabel.Services.Models
{
    public class FolderModel
    {
        public string Caption { get; set; }
        public string Filename { get; set; }
        public bool IncludeSubFolders { get; set; }
        public string Path { get; set; }
    }
}