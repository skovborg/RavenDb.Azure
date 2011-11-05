using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NLog;
using Raven.Abstractions.Replication;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Database;
using Raven.Json.Linq;
using RavenDb.Bundles.Azure.Configuration;

namespace RavenDb.Bundles.Azure.Replication
{
    [Export(typeof(IReplicationProvider))]
    public class CloudReplicationProvider : IReplicationProvider
    {
        private static readonly Logger          log = LogManager.GetCurrentClassLogger();

        [Import]
        public IConfigurationProvider           ConfigurationProvider { get; set; }

        [Import]
        public IInstanceEnumerator              InstanceEnumerator { get; set; }

        [ImportMany]
        public IEnumerable<IReplicationRecipe>  Recipes { get; set; } 

        public void ReplicateDefaultDatabase( DocumentDatabase database )
        {
            InstanceDescription self = null;
            var replicationTargets = GetReplicationTargets(out self);

            if (replicationTargets != null)
            {
                log.Info("Ensuring default database is replicated from {0} at {1}", self.Id, self.InternalUrl);

                var documentId = new ReplicationDocument().Id;

                var replicationDocument = new ReplicationDocument()
                {
                    Destinations =
                        replicationTargets
                        .Select(i => new ReplicationDestination() { Url = i.InternalUrl })
                        .ToList()
                };

                database.Put(documentId, null, RavenJObject.FromObject(replicationDocument), new RavenJObject(), null);
            }
        }

        public void ReplicateTenantDatabase( string databaseName )
        {
            InstanceDescription self = null;
            var replicationTargets = GetReplicationTargets(out self);

            if (replicationTargets != null)
            {
                EnsureDatabaseExists(replicationTargets,databaseName);

                // Setup replication:
                using (var documentStore = new DocumentStore() { Url = self.InternalUrl })
                {
                    log.Info("Ensuring database {0} is replicated from {1} at {2}", databaseName, self.Id, self.InternalUrl);

                    documentStore.Initialize();

                    using (var session = documentStore.OpenSession(databaseName))
                    {
                        var documentId = new ReplicationDocument().Id; // Just to stay in sync with changes from RavenDb

                        var replicationDocument = session.Load<ReplicationDocument>(documentId) ?? new ReplicationDocument();

                        replicationDocument.Destinations = replicationTargets.Select(i => new ReplicationDestination() { Url = string.Format("{0}/databases/{1}", i.InternalUrl, databaseName) }).ToList();
                        session.Store(replicationDocument);
                        session.SaveChanges();
                    }
                }
            }
        }

        private void EnsureDatabaseExists(IEnumerable<InstanceDescription> replicationTargets,string databaseName)
        {
            // Ensure database exists:
            foreach (var instance in replicationTargets)
            {
                using (var documentStore = new DocumentStore() { Url = instance.InternalUrl })
                {
                    log.Info("Ensuring database {0} exists on instance {1} at {2}", databaseName, instance.Id,
                                instance.InternalUrl);

                    documentStore.Initialize();
                    documentStore.DatabaseCommands.EnsureDatabaseExists(databaseName);
                }
            }
        }

        private IEnumerable<InstanceDescription> GetReplicationTargets( out InstanceDescription self )
        {
            var recipe  = GetRecipe();
            self        = InstanceEnumerator.GetSelf();

            if (recipe != null)
            {
                return recipe.GetReplicationTargets(self, InstanceEnumerator.GetOthers());
            }

            log.Warn("No replication recipe selected");
            return null;
        }

        private IReplicationRecipe GetRecipe()
        {
            var recipeName = ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.ReplicationRecipe, "SeperateReadersAndWriters");
            return Recipes.FirstOrDefault(r => r.GetType().Name.Replace("Recipe",string.Empty).Equals(recipeName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
