using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NLog;
using Raven.Abstractions.Data;
using Raven.Abstractions.Indexing;
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

        public void ReplicateDatabaseCreation( DocumentDatabase database )
        {
            InstanceDescription self = null;
            var replicationTargets = GetReplicationTargets(out self);

            if (replicationTargets != null)
            {
                log.Info("Ensuring default database {0} is replicated from {2} at {3}",string.IsNullOrWhiteSpace(database.Name) ? "Default" : database.Name, self.Id, self.InternalUrl);

                if (!string.IsNullOrWhiteSpace(database.Name))
                {
                    EnsureDatabaseExists(replicationTargets,database.Name);
                }

                var documentId = new ReplicationDocument().Id;

                var replicationDocument = new ReplicationDocument()
                {
                    Destinations =
                        replicationTargets
                        .Select(i => new ReplicationDestination() { Url = GetReplicationUrl(database.Name,i) })
                        .ToList()
                };

                database.Put(documentId, null, RavenJObject.FromObject(replicationDocument), new RavenJObject(), null);
            }
        }

        public void ReplicateDatabaseDeletion(string databaseName)
        {
            InstanceDescription self = null;
            var replicationTargets = GetReplicationTargets(out self);

            if (replicationTargets != null)
            {
                foreach (var replicationTarget in replicationTargets)
                {
                    log.Info("Ensuring database {0} is deletet on {1} at {2}, from {3} at {4}", databaseName, replicationTarget.Id, replicationTarget.RoleInstanceIndex,self.Id,self.InternalUrl);

                    using (var documentStore = new DocumentStore() {Url = replicationTarget.InternalUrl})
                    {
                        documentStore.Initialize();

                        using (var session = documentStore.OpenSession())
                        {
                            var databaseDocument = session.Load<DatabaseDocument>("Raven/Databases/" + databaseName);
                            if (databaseDocument != null)
                            {
                                session.Delete(databaseDocument);
                                session.SaveChanges();
                            }
                        }
                    }
                }
            }
        }

        public void ReplicateIndices( string databaseName,IEnumerable<IndexDefinition> indicesToReplicate )
        {
            InstanceDescription self = null;
            var replicationTargets = GetReplicationTargets(out self);

            if (replicationTargets != null)
            {
                foreach (var replicationTarget in replicationTargets)
                {
                    using (var documentStore = new DocumentStore() { Url = replicationTarget.InternalUrl })
                    {
                        documentStore.Initialize();

                        using (var session = string.IsNullOrWhiteSpace(databaseName) ? documentStore.OpenSession() : documentStore.OpenSession(databaseName))
                        {
                            session.Advanced.MaxNumberOfRequestsPerSession = int.MaxValue;

                            foreach (var index in indicesToReplicate)
                            {
                                log.Info("Checking if replication is needed for index {0} exists on server {1} at {2}",index.Name,replicationTarget.Id,replicationTarget.InternalUrl);

                                var otherIndexDefinition = session.Advanced.DatabaseCommands.GetIndex(index.Name);

                                if (otherIndexDefinition == null)
                                {
                                    log.Info("Index {0} does not exist on server {1} at {2}", index.Name, replicationTarget.Id, replicationTarget.InternalUrl);
                                    session.Advanced.DatabaseCommands.PutIndex(index.Name, index);
                                }
                                else
                                {
                                    if (!otherIndexDefinition.Equals(index))
                                    {
                                        log.Info("Index {0} is not the same as on server {1} at {2}", index.Name, replicationTarget.Id, replicationTarget.InternalUrl);
                                        session.Advanced.DatabaseCommands.PutIndex(index.Name, index);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private string GetReplicationUrl(string databaseName, InstanceDescription destination)
        {
            var baseUrl = ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.ReplicationUsePublicEndpoint, true)
                              ? destination.ExternalUrl
                              : destination.InternalUrl;

            return databaseName == null ? baseUrl : string.Format("{0}/databases/{1}", baseUrl, databaseName);
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
            var recipeName = ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.ReplicationRecipe, "PeerToPeer");
            return Recipes.FirstOrDefault(r => r.GetType().Name.Replace("Recipe",string.Empty).Replace("Replication",string.Empty).Equals(recipeName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
