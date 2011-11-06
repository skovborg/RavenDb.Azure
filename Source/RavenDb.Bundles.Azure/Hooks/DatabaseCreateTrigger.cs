using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Replication;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Database.Plugins;
using Raven.Json.Linq;
using RavenDb.Bundles.Azure.Configuration;
using RavenDb.Bundles.Azure.Replication;
using RavenDb.Bundles.Azure.Storage;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class DatabaseCreateTrigger : AbstractPutTrigger
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        [Import]
        public IConfigurationProvider   ConfigurationProvider { get; set; }

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        public IStorageProvider         StorageProvider { get; set; }

        [Import]
        public IReplicationProvider     ReplicationProvider { get; set; }

        public override void OnPut(string key, RavenJObject document, RavenJObject metadata, Raven.Abstractions.Data.TransactionInformation transactionInformation)
        {
            string databaseName = null;

            if (DocumentUtilities.TryGetDatabaseNameFromKey(key, out databaseName))
            {
                var dataDirectory = StorageProvider.GetDirectoryForDatabase(databaseName);

                RavenJToken settingsToken = null;

                if (document.TryGetValue("Settings", out settingsToken))
                {
                    var settingsObject = settingsToken as RavenJObject;

                    if (settingsObject != null)
                    {
                        settingsObject["Raven/DataDir"] = new RavenJValue(dataDirectory.FullName);
                    }
                }
            }

            base.OnPut(key, document, metadata, transactionInformation);
        }

        public override void AfterCommit(string key, RavenJObject document, RavenJObject metadata, Guid etag)
        {
            string databaseName = null;

            if (DocumentUtilities.TryGetDatabaseNameFromKey(key, out databaseName))
            {
                if (ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.ReplicationExecuteReplicationSetup, true))
                {
                    ReplicationProvider.SetupTenantDatabaseReplication(databaseName);
                }
            }

            base.AfterCommit(key, document, metadata, etag);
        }
    }
}
