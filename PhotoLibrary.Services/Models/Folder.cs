﻿using System.Collections.Generic;

namespace PhotoLabel.Services.Models
{
    public class Folder
    {
        public string Filename { get; set; }
        public bool IsSelected { get; set; }
        public string Path { get; set; }
        public List<string> SelectedSubFolders { get; set; }
        public override string ToString()
        {
            return $@"Folder - ""{Path}""";
        }
    }
}