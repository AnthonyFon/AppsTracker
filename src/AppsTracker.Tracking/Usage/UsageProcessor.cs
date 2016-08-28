﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using AppsTracker.Common.Utils;
using AppsTracker.Data.Models;
using AppsTracker.Data.Repository;
using AppsTracker.Domain.Tracking;

namespace AppsTracker.Tracking.Helpers
{
    [Export(typeof(IUsageProcessor))]
    internal sealed class UsageProcessor : IUsageProcessor
    {
        private readonly IRepository repository;
        private readonly ITrackingService trackingService;
        private readonly IDictionary<UsageTypes, Usage> usageTypesMap = new Dictionary<UsageTypes, Usage>();

        private Usage loginUsage;

        [ImportingConstructor]
        public UsageProcessor(IRepository repository,
                              ITrackingService trackingService)
        {
            this.repository = repository;
            this.trackingService = trackingService;
        }

        public Usage LoginUser(int userId)
        {
            var login = new Usage() { UserID = userId, UsageEnd = DateTime.Now, UsageType = UsageTypes.Login, IsCurrent = true };
            repository.SaveNewEntity(login);
            loginUsage = login;
            return login;
        }

        public void NewUsage(UsageTypes usageType)
        {
            Ensure.Condition<InvalidOperationException>(usageTypesMap.ContainsKey(usageType) == false, "Usage type exists");
            var usage = new Usage(trackingService.UserID, usageType) { SelfUsageID = trackingService.UsageID };
            usageTypesMap.Add(usageType, usage);
        }

        public void UsageEnded(UsageTypes usageType)
        {
            if (usageTypesMap.ContainsKey(usageType) == false)
                return;

            var usage = usageTypesMap[usageType];
            SaveUsage(usageType, usage);
        }

        private void SaveUsage(UsageTypes usageType, Usage usage)
        {
            usage.UsageEnd = DateTime.Now;
            usage.IsCurrent = false;
            usageTypesMap.Remove(usageType);
            repository.SaveNewEntity(usage);
        }

        public void EndAllUsages()
        {
            var usagesCopy = usageTypesMap.Values.ToList();
            foreach (var usage in usagesCopy)
            {
                SaveUsage(usage.UsageType, usage);
            }
            loginUsage.IsCurrent = false;
            loginUsage.UsageEnd = DateTime.Now;
            repository.SaveModifiedEntity(loginUsage);
        }
    }
}
