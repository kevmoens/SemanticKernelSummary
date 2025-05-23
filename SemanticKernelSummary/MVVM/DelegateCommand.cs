using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SemanticKernelSummary.MVVM
{
    public class DelegateCommand(Action action, Func<bool>? canExecute = null) : ICommand
    {
        private readonly Action _action = action;
        private readonly Func<bool>? _canExecute = canExecute;

#pragma warning disable CS0414 // The event 'DelegateCommand.CanExecuteChanged' is never used
#pragma warning disable CS0067 // The event 'DelegateCommand.CanExecuteChanged' is never used
		public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067 // The event 'DelegateCommand.CanExecuteChanged' is never used
#pragma warning restore CS0414 // The event 'DelegateCommand.CanExecuteChanged' is never used

		public bool CanExecute(object? parameter)
        {
            if (_canExecute is null)
            {
                return true;
            }
            return _canExecute!.Invoke();
        }

        public void Execute(object? parameter)
        {
            _action.Invoke();
        }
    }

    public class DelegateCommand<T>(Action<T?> action, Func<bool>? canExecute = null) : ICommand
    {
        private readonly Action<T?> _action = action;
        private readonly Func<bool>? _canExecute = canExecute;

#pragma warning disable CS0414 // The event 'DelegateCommand.CanExecuteChanged' is never used
#pragma warning disable CS0067 // The event 'DelegateCommand.CanExecuteChanged' is never used
		public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067 // The event 'DelegateCommand.CanExecuteChanged' is never used
#pragma warning restore CS0414 // The event 'DelegateCommand.CanExecuteChanged' is never used

		public bool CanExecute(object? parameter)
        {
            if (_canExecute is null)
            {
                return true;
            }
            return _canExecute!.Invoke();
        }

        public void Execute(object? parameter)
        {
            if (parameter is null)
			{
				_action.Invoke(default);
				return;
			}
			_action.Invoke((T)parameter);
        }
    }
}
