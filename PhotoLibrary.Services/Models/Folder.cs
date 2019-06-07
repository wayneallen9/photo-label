using System.Collections.Generic;

namespace PhotoLabel.Services.Models
{
    public class Folder
    {
        public string Caption { get; set; }
        public string Filename { get; set; }
        public bool IsSelected { get; set; }
        public string Path { get; set; }
        public List<Folder> SubFolders { get; set; }
        public override string ToString()
        {
            return $@"Directory - ""{Path}""";
        }
    }
}