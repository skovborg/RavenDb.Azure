using System.Collections.Generic;
using Raven.Abstractions.Indexing;
using Raven.Database;

namespace RavenDb.Bundles.Azure.Replication
{
    public interface IReplicationProvider
    {
        void SetupDefaultDatabaseReplication(DocumentDatabase database);
        void SetupTenantDatabaseReplication(string databaseName);

        void ReplicateTenantDatabaseDeletion(string databaseName);
        void ReplicateIndices(string databaseName, IEnumerable<IndexDefinition> indicesToReplicate);
    }
}