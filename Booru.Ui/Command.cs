using System;
using System.Windows.Input;

namespace Booru.Ui
{
    public class Command : UiBindableBase, ICommand
    {
        private Action<object> _Execute;

        private Predicate<object> _CanExecute;

        bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set
            {
                _IsEnabled = true;
                NotifyPropertyChanged(nameof(IsEnabled));
                OnCanExecuteChanged();
            }
        }

        private event EventHandler CanExecuteChangedInternal;

        public Command(Action<object> execute) : this(execute, DefaultCanExecute) { }

        public Command(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            if (canExecute == null)
                throw new ArgumentNullException("canExecute");

            _Execute = execute;
            _CanExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                CanExecuteChangedInternal += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
                CanExecuteChangedInternal -= value;
            }
        }

        public bool CanExecute(object parameter) => IsEnabled && _CanExecute != null && _CanExecute(parameter);

        public void Execute(object parameter) => _Execute(parameter);

        private static bool DefaultCanExecute(object parameter) => true;

        public void OnCanExecuteChanged() => CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
    }
}
