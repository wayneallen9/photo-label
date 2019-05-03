namespace PhotoLabel.Services
{
    public interface IDialogService
    {
        string Browse(string description, string defaultFolder);
        bool Confirm(string text, string caption);
        void Error(string text);
    }
}