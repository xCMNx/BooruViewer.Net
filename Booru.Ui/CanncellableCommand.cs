using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Booru.Ui
{
    public class CancellableCommand : UiBindableBase, ICommand
    {
        public static CancellationToken GlobalCancellactionToken = CancellationToken.None;

        private Action<object, CancellationToken> _Execute;

        private Predicate<object> _CanExecute;

        bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set
            {
                _IsEnabled = value;
                NotifyPropertyChanged(nameof(IsEnabled));
                OnCanExecuteChanged();
            }
        }

        bool _IsExecuting = false;
        public bool IsExecuting
        {
            get { return _IsExecuting; }
            private set
            {
                _IsExecuting = value;
                NotifyPropertyChanged(nameof(IsExecuting));
                OnCanExecuteChanged();
            }
        }

        private event EventHandler CanExecuteChangedInternal;

        CancellationTokenSource _TokenSource;

        public CancellableCommand(Action<object, CancellationToken> execute) : this(execute, DefaultCanExecute) { }

        public CancellableCommand(Action<object, CancellationToken> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            if (canExecute == null)
                throw new ArgumentNullException("canExecute");

            _Execute = execute;
            _CanExecute = canExecute;
        }

        public void Cancell()
        {
            _TokenSource.Cancel();
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

        public bool CanExecute(object parameter) => IsEnabled && !_IsExecuting && _CanExecute != null && _CanExecute(parameter);

        public async void Execute(object parameter)
        {
            IsExecuting = true;
            _TokenSource = CancellationTokenSource.CreateLinkedTokenSource(GlobalCancellactionToken);
            await Task.Factory.StartNew(() => _Execute(parameter, _TokenSource.Token), _TokenSource.Token);
            IsExecuting = false;
        }

        private static bool DefaultCanExecute(object parameter) => true;

        public void OnCanExecuteChanged() => CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
    }
}
