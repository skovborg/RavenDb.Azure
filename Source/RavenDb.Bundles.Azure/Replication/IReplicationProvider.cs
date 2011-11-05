using System.Collections.Generic;
using Raven.Abstractions.Indexing;
using Raven.Database;

namespace RavenDb.Bundles.Azure.Replication
{
    public interface IReplicationProvider
    {
        void ReplicateDefaultDatabase(DocumentDatabase database);
        void ReplicateTenantDatabase(string databaseName);
        void ReplicateIndices(string databaseName, IEnumerable<IndexDefinition> indicesToReplicate);
    }
}