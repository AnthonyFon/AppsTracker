﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Data.Entity;
using Task_Logger_Pro.Controls;
using Task_Logger_Pro.Logging;
using Task_Logger_Pro.MVVM;
using AppsTracker.DAL;
using AppsTracker.Models.EntityModels;
using AppsTracker.DAL.Repos;

namespace Task_Logger_Pro.Pages.ViewModels
{
    class Data_keystrokesViewModel : ViewModelBase, IChildVM, IWorker, ICommunicator
    {
        #region Fields

        bool _working;

        IEnumerable<Log> _logList;


        #endregion

        #region Properties

        public string Title
        {
            get
            {
                return "KEYSTROKES";
            }
        }
        public bool IsContentLoaded
        {
            get;
            private set;
        }
        public IEnumerable<Log> LogList
        {
            get
            {
                return _logList;
            }
            set
            {
                _logList = value;
                PropertyChanging("LogList");
            }
        }

        public bool Working
        {
            get
            {
                return _working;
            }
            set
            {
                _working = value;
                PropertyChanging("Working");
            }
        }

        public Mediator Mediator
        {
            get { return Mediator.Instance; }
        }

        #endregion

        public Data_keystrokesViewModel()
        {
            Mediator.Register(MediatorMessages.RefreshLogs, new Action(LoadContent));
        }

        public async void LoadContent()
        {
            Working = true;
            LogList = await GetContentFromRepo();
            Working = false;
            IsContentLoaded = true;
        }


        private Task<IEnumerable<Log>> GetContentFromRepo()
        {
            return LogRepo.Instance.GetFilteredAsync(l => l.KeystrokesRaw != null
                                                        && l.DateCreated >= Globals.Date1
                                                        && l.DateCreated <= Globals.Date2
                                                        && l.Window.Application.UserID == Globals.SelectedUserID
                                                        ,l=>l.Window.Application
                                                        ,l=>l.Screenshots);
        }      
    }
}
