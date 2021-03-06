﻿#region Licence
/*
  *  Author: Marko Devcic, madevcic@gmail.com
  *  Copyright: Marko Devcic, 2015
  *  Licence: http://creativecommons.org/licenses/by-nc-nd/4.0/
 */
#endregion

using System;
using System.Diagnostics;
using System.Windows.Input;
using AppsTracker.Domain.Model;

namespace AppsTracker.MVVM
{
    public abstract class ViewModelBase : ObservableObject, IWorker
    {
        private bool working;

        private readonly object _lock = new object();

        public abstract string Title { get; }

        public bool Working
        {
            get
            {
                lock (_lock)
                    return working;
            }
            set
            {
                lock (_lock)
                {
                    SetPropertyValue(ref working, value);
                }
            }
        }
        

        private bool windowClose;

        public bool WindowClose
        {
            get { return windowClose; }
            set { SetPropertyValue(ref windowClose, value); }
        }



        private ICommand closeWindowCommand;

        public ICommand CloseWindowCommand
        {
            get { return closeWindowCommand ?? (closeWindowCommand = new DelegateCommand(CloseWindow)); }
        }


        private void CloseWindow()
        {
            WindowClose = true;
        }


#if DEBUG
        ~ViewModelBase()
        {
            Debug.WriteLine(string.Format("{0}, {1}, {2} Finalized", this.GetType().Name, this.GetType().FullName, this.GetHashCode()));
        }
#endif
    }

}
