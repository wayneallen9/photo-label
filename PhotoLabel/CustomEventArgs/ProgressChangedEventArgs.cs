namespace PhotoLabel.CustomEventArgs
{
    public class ProgressChangedEventArgs
    {
        public int Count { get; set; }
        public int Current { get; set; }
        public string Directory { get; set; }
    }
}