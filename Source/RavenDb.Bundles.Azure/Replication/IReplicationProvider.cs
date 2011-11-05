using Raven.Database;

namespace RavenDb.Bundles.Azure.Replication
{
    public interface IReplicationProvider
    {
        void ReplicateDefaultDatabase(DocumentDatabase database);
        void ReplicateTenantDatabase(string databaseName);
    }
}