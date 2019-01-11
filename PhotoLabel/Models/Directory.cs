namespace PhotoLabel.Models
{
    public class Directory
    {
        public string Caption { get; set; }
        public string Filename { get; set; }
        public string Path { get; set; }
        public override string ToString()
        {
            return $@"Directory ""{Path}""";
        }
    }
}