using System;
using System.Windows.Input;

namespace PhotoLabel.Wpf
{
    public class CommandHandler : ICommand
    {
        #region variables
        private readonly Action _action;
        private readonly bool _canExecute;
        #endregion

        public CommandHandler(Action action, bool canExecute)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }
}