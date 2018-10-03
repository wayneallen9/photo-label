using System;
using System.Collections.Generic;
using System.Drawing;
namespace PhotoLabel.ViewModels
{
    public interface IObserver
    {
        void OnError(Exception error);
        void OnOpen(IList<string> filenames);
        void OnPreview(string filename, Image preview);
        void OnUpdate(MainFormViewModel value);
    }
}