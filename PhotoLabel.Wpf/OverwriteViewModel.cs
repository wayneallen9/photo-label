using System;
using PhotoLabel.Wpf.Annotations;
using Shared;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PhotoLabel.Wpf
{
    public class OverwriteViewModel : INotifyPropertyChanged
    {
        #region enumerations

        public enum Actions
        {
            Overwrite,
            Skip
        }
        #endregion

        #region variables

        private Actions _action;
        private string _filename;
        private readonly ILogger _logger;
        private ICommand _overwriteCommand;
        private bool _remember;
        private ICommand _skipCommand;
        #endregion

        public OverwriteViewModel(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;

            // initialise variables
            _action = Actions.Skip;
        }

        public Actions Action
        {
            get => _action;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(Action)} has changed...");
                    if (_action == value)
                    {
                        logger.Trace($"Value of {nameof(Action)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting value of {nameof(Action)} to {value}...");
                    _action = value;

                    OnPropertyChanged();
                }
            }
        }

        public string Filename
        {
            get => _filename;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(Filename)} has changed...");
                    if (_filename == value)
                    {
                        logger.Trace($"Value of {nameof(Filename)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(Filename)} to ""{value}""...");
                    _filename = value;

                    OnPropertyChanged();
                }
            }
        }

        private void Overwrite(Window window)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($"Setting {nameof(Action)} to {Actions.Overwrite}...");
                Action = Actions.Overwrite;

                logger.Trace($"Setting dialog result...");
                window.DialogResult = true;

                logger.Trace("Closing window...");
                window.Close();
            }
        }

        public ICommand OverwriteCommand =>
            _overwriteCommand ?? (_overwriteCommand = new CommandHandler<Window>(Overwrite, true));

        public bool Remember
        {
            get => _remember;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(Remember)} has changed...");
                    if (_remember == value)
                    {
                        logger.Trace($"Value of {nameof(Remember)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting value of {nameof(Remember)} to {value}...");
                    _remember = value;

                    OnPropertyChanged();
                }
            }
        }

        private void Skip(Window window)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($"Setting {nameof(Action)} to {Actions.Skip}...");
                Action = Actions.Skip;

                logger.Trace($"Setting dialog result...");
                window.DialogResult = true;

                logger.Trace("Closing window...");
                window.Close();
            }
        }

        public ICommand SkipCommand => _skipCommand ?? (_skipCommand = new CommandHandler<Window>(Skip, true));

        public string Title => $"{Properties.Resources.ApplicationName} - [Overwrite File?]";

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Invoking event handlers...");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
