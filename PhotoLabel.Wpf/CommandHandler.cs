using System;
using System.Windows.Input;

namespace PhotoLabel.Wpf
{
    public interface ICommandHandler
    {
        void Notify();
    }

    public class CommandHandler : ICommand, ICommandHandler
    {
        #region events
        public event EventHandler CanExecuteChanged;
        #endregion

        #region variables
        private readonly Action _action;
        private readonly bool _canExecute;
        private readonly Func<bool> _canExecuteFunction;
        #endregion

        public CommandHandler(Action action, Func<bool> canExecuteFunction)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _canExecute = true;
            _canExecuteFunction = canExecuteFunction;
        }

        public CommandHandler(Action action, bool canExecute)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteFunction == null ? _canExecute : _canExecuteFunction();
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public void Notify()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class CommandHandler<T> : ICommand
    {
        #region variables
        private readonly Action<T> _action;
        private readonly bool _canExecute;
        #endregion

        public CommandHandler(Action<T> action, bool canExecute)
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
            _action((T)parameter);
        }
    }
}