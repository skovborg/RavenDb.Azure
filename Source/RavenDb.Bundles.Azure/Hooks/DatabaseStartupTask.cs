﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;
using Raven.Database.Plugins;
using RavenDb.Bundles.Azure.Configuration;
using RavenDb.Bundles.Azure.Diagnostics;
using RavenDb.Bundles.Azure.Replication;
using RavenDb.Bundles.Azure.Storage;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class DatabaseStartupTask : IStartupTask
    {
        private static readonly Logger  log = LogManager.GetCurrentClassLogger();

        [Import]
        public IConfigurationProvider   ConfigurationProvider { get; set; }

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        public IDiagnosticsProvider     DiagnosticsProvider { get; set; }

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        public IStorageProvider         StorageProvider     { get; set; }

        [Import]
        public IReplicationProvider     ReplicationProvider { get; set; }
       
        public void Execute(Raven.Database.DocumentDatabase database)
        {
            // First step setup diagnostics:
            DiagnosticsProvider.Initialize();

            // And then storage:
            StorageProvider.Initialize();

            var storageDirectory = StorageProvider.GetDirectoryForDatabase(database.Name);

            log.Info("Setting storage directory for database {0} to {1}",string.IsNullOrWhiteSpace(database.Name) ? "Default" : database.Name,storageDirectory.FullName);
            database.Configuration.DataDirectory = storageDirectory.FullName;

            if (ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.ReplicationDatabaseCreation, true))
            {
                ReplicationProvider.ReplicateDatabaseCreation(database);
            }
        }
    }
}
