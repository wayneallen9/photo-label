using System;

namespace PhotoLabel.Models
{
    public class PreviewModel
    {
        public DateTime DateCreated { get; set; }
        public string Filename { get; set; }
        public bool IsPreviewLoaded { get; set; }
    }
}