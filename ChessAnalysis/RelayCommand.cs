using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChessAnalysis
{

    public class RelayCommand : ICommand
    {
       
        // ← genau diese beiden Felder
        private readonly Action<object?> _executeParam;
        private readonly Func<object?, bool>? _canExecuteParam;

        /// <summary>
        /// Konstruktor für Commands mit Parameter
        /// </summary>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteParam = canExecute;
        }

        /// <summary>
        /// Overload für parameterlose Actions
        /// </summary>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(
                // parameterlose Action → Action<object?>
                obj => execute(),
                // Func<bool> → Func<object?,bool>
                canExecute is null
                    ? (Func<object?, bool>?)null
                    : new Func<object?, bool>(_ => canExecute())
              )
        { }

        // WPF hook, damit Buttons automatisch neu enabled/disabled werden
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
            => _canExecuteParam?.Invoke(parameter) ?? true;

        public void Execute(object? parameter)
            => _executeParam(parameter);

        /// <summary>
        /// Manuell CanExecuteChanged auslösen (falls benötigt)
        /// </summary>
        public void RaiseCanExecuteChanged()
            => CommandManager.InvalidateRequerySuggested();
    }
}
    


