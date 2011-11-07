using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Raven.Database.Plugins;
using RavenDb.Bundles.Azure.Configuration;
using RavenDb.Bundles.Azure.Replication;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class DatabaseDeleteTrigger : AbstractDeleteTrigger 
    {
        [Import]
        public IConfigurationProvider   ConfigurationProvider { get; set; }

        [Import]
        public IReplicationProvider     ReplicationProvider { get; set; }

        public override void AfterCommit(string key)
        {
            string databaseName = null;

            if (DocumentUtilities.TryGetDatabaseNameFromKey(key, out databaseName))
            {
                if (ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.ReplicationDatabaseDeletion, true))
                {
                    ReplicationProvider.ReplicateDatabaseDeletion(databaseName);
                }
            }

            base.AfterCommit(key);
        }
    }
}
