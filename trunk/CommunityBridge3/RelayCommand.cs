using System;
using System.Windows.Input;

namespace CommunityBridge3
{
    // See: http://docs.telerik.com/data-access/quick-start-scenarios/wpf/quickstart-wpf-viewmodelbase-and-relaycommand
    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        private readonly Action _methodToExecute;
        private readonly Func<bool> _canExecuteEvaluator;
        public RelayCommand(Action methodToExecute, Func<bool> canExecuteEvaluator)
        {
            this._methodToExecute = methodToExecute;
            this._canExecuteEvaluator = canExecuteEvaluator;
        }
        public RelayCommand(Action methodToExecute)
            : this(methodToExecute, null)
        {
        }
        public bool CanExecute(object parameter)
        {
            if (this._canExecuteEvaluator == null)
            {
                return true;
            }
            bool result = this._canExecuteEvaluator.Invoke();
            return result;
        }
        public void Execute(object parameter)
        {
            this._methodToExecute.Invoke();
        }
    }
}
