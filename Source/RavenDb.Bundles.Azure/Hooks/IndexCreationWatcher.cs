using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NLog;
using Raven.Abstractions.Indexing;
using Raven.Database;
using Raven.Database.Config;
using Raven.Database.Plugins;
using Raven.Json.Linq;
using RavenDb.Bundles.Azure.Configuration;
using RavenDb.Bundles.Azure.Replication;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class IndexCreationWatcher : AbstractBackgroundTask
    {
        private static readonly Logger  log = LogManager.GetCurrentClassLogger();

        [Import]
        public IConfigurationProvider   ConfigurationProvider { get; set; }

        [Import]
        public IReplicationProvider     ReplicationProvider { get; set; }

        protected override TimeSpan     TimeoutForNextWork  ()
        {
            return
                TimeSpan.FromMinutes(
                    ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.ReplicationIndexPollPeriodInMinutes, 1.0));
        }

        protected override bool         HandleWork()
        {
            log.Info("Scanning indices in database {0} for replication",string.IsNullOrWhiteSpace(Database.Name) ? "Default" : Database.Name);

            var indexNames              = Database.GetIndexNames(0, int.MaxValue).OfType<RavenJValue>().Select(t => t.Value.ToString());
            var indexNamesToReplicate   = indexNames.Where(name => !name.StartsWith("Raven/") && !name.StartsWith("Temp/"));
            var indicesToReplicate      = indexNamesToReplicate.Select(name => Database.GetIndexDefinition(name)).ToArray();

            if (ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.ReplicationIndexCreation, true))
            {
                ReplicationProvider.ReplicateIndices(Database.Name, indicesToReplicate);
            }

            return false;
        }
    }
}
