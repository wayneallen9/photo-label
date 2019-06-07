namespace PhotoLabel.Services
{
    public interface IDialogService
    {
        /// <summary>
        /// Show the folder browse dialog.
        /// </summary>
        /// <param name="description">The description to display on the dialog.</param>
        /// <param name="defaultFolder">The path to the folder the dialog should show when opening.</param>
        /// <returns>The path selected by the user, or null if the user cancels.</returns>
        string Browse(string description, string defaultFolder);
        bool Confirm(string text, string caption);
        void Error(string text);
    }
}