﻿using System;
using System.Collections.Generic;
using System.Windows.Input;

using AppsTracker.MVVM;
using AppsTracker.Models.ChartModels;
using AppsTracker.DAL.Service;


namespace AppsTracker.Pages.ViewModels
{
    internal sealed class Statistics_appUsageViewModel : ViewModelBase, ICommunicator
    {
        #region Fields

        private MostUsedAppModel _mostUsedAppModel;

        private AsyncProperty<IEnumerable<MostUsedAppModel>> _mostUsedAppsList;

        private AsyncProperty<IEnumerable<DailyAppModel>> _dailyAppList;

        private ICommand _returnFromDetailedViewCommand;

        private IChartService _service;

        #endregion

        #region Properties

        public override string Title
        {
            get
            {
                return "APPS";
            }
        }

        public MostUsedAppModel MostUsedAppModel
        {
            get
            {
                return _mostUsedAppModel;
            }
            set
            {
                _mostUsedAppModel = value;
                PropertyChanging("MostUsedAppModel");
                _dailyAppList.Reload();
            }
        }

        public object SelectedItem
        {
            get;
            set;
        }


        public AsyncProperty<IEnumerable<MostUsedAppModel>> MostUsedAppsList
        {
            get
            {
                return _mostUsedAppsList;
            }
        }
        public AsyncProperty<IEnumerable<DailyAppModel>> DailyAppList
        {
            get
            {
                return _dailyAppList;
            }
        }
        public ICommand ReturnFromDetailedViewCommand
        {
            get
            {
                return _returnFromDetailedViewCommand == null ? _returnFromDetailedViewCommand = new DelegateCommand(ReturnFromDetailedView) : _returnFromDetailedViewCommand;
            }
        }

        public IMediator Mediator
        {
            get { return MVVM.Mediator.Instance; }
        }

        #endregion

        private void ReturnFromDetailedView()
        {
            MostUsedAppModel = null;
        }

        public Statistics_appUsageViewModel()
        {            
            _service = ServiceFactory.Get<IChartService>();

            _mostUsedAppsList = new AsyncProperty<IEnumerable<MostUsedAppModel>>(GetContent, this);
            _dailyAppList = new AsyncProperty<IEnumerable<DailyAppModel>>(GetSubContent, this);

            Mediator.Register(MediatorMessages.RefreshLogs, new Action(ReloadAll));
        }

        private void ReloadAll()
        {
            _mostUsedAppsList.Reload();
            _dailyAppList.Reload();
        }
        private IEnumerable<MostUsedAppModel> GetContent()
        {
            return _service.GetMostUsedApps(Globals.SelectedUserID, Globals.Date1, Globals.Date2);
        }

        private IEnumerable<DailyAppModel> GetSubContent()
        {
            var model = _mostUsedAppModel;
            if (model == null)
                return null;

            return _service.GetSingleMostUsedApp(Globals.SelectedUserID, model.AppName, Globals.Date1, Globals.Date2);
        }
    }
}