﻿using System;
using AppsTracker.Communication;

namespace AppsTracker.Tests.Fakes
{
    public sealed class SyncContextMock : ISyncContext
    {
        public System.Threading.SynchronizationContext Context
        {
            get;
            set;
        }

        public void Invoke(System.Threading.SendOrPostCallback method, object state = null)
        {
            method.Invoke(state);
        }

        public void Invoke(Action action)
        {
            action.Invoke();
        }
    }
}
