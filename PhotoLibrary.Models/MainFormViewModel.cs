using System;
using System.Collections.Generic;
using System.Drawing;
namespace PhotoLibrary.Models
{
    public class MainFormViewModel
    {
        #region variables
        private readonly RecentlyUsedFiles _recentlyUsedFiles;
        #endregion

        public MainFormViewModel()
        {
            _recentlyUsedFiles = Properties.Settings.Default.RecentlyUsedFiles ?? new RecentlyUsedFiles();
        }

        public Color Color
        {
            get
            {
                try
                {
                    return Properties.Settings.Default.Color;
                }
                catch (NullReferenceException)
                {
                    return Color.White;
                }
            }
            set
            {
                // save the color
                Properties.Settings.Default.Color = value;

                // persist the changes
                Properties.Settings.Default.Save();
            }
        }

        public Font Font {
            get => Properties.Settings.Default.Font ?? SystemFonts.DefaultFont;
            set {
                // save the new font
                Properties.Settings.Default.Font = value;

                // persit the change
                Properties.Settings.Default.Save();
            }
        }

        public List<ImageViewModel> Images { get; set; }

        public RecentlyUsedFiles RecentlyUsedFiles
        {
            get => _recentlyUsedFiles;
        }

        public void Save()
        {
            // save the list of recently used files
            Properties.Settings.Default.RecentlyUsedFiles = _recentlyUsedFiles;

            // persist the settings
            Properties.Settings.Default.Save();
        }
        public int Zoom {
            get => Properties.Settings.Default.Zoom;
            set {
                // save the new value
                Properties.Settings.Default.Zoom = value;

                // persist the changes
                Properties.Settings.Default.Save();
            }
        }
    }
}