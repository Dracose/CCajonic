using System;
using System.Windows;
using System.Windows.Input;

namespace Cajonic.Services
{
    public class CommandHandler : ICommand
    {
        private readonly Action mAction;
        private readonly Func<bool> mCanExecute;
        
        public CommandHandler(Action action, Func<bool> canExecute)
        {
            mAction = action;
            mCanExecute = canExecute;
        }
        
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        
        public bool CanExecute(object parameter)
        {
            return mCanExecute.Invoke();
        }

        public void Execute(object parameter)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                mAction();
            });
        }
    }
    
    public class RelayCommand : ICommand
    {
        private readonly Action<object> mAction;
        private readonly bool mCanExecute;
        public RelayCommand(Action<object> action, bool canExecute)
        {
            mAction = action;
            mCanExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return mCanExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            mAction(parameter);
        }
    }
}