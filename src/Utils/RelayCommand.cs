/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/

using System;
using System.Windows.Input;

namespace OneTouchMonitor.Utils
{
    public class RelayCommand : ICommand
    {
        private Action<object> action;
        private bool canExecute;
        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> action)
        {
            this.action = action;
            this.canExecute = true;
        }
        public RelayCommand(Action<object> action, bool canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute;
        }

        public void Execute(object parameter)
        {
            action(parameter);
        }
    }
}
