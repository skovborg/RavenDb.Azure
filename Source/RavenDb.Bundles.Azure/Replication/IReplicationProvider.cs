using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Raven.Abstractions.Indexing;
using Raven.Database;

namespace RavenDb.Bundles.Azure.Replication
{
    [ContractClass(typeof(ReplicationProviderContracts))]
    public interface IReplicationProvider
    {
        void ReplicateDatabaseCreation(DocumentDatabase database);
        void ReplicateDatabaseDeletion(string databaseName);
        void ReplicateIndices(string databaseName, IEnumerable<IndexDefinition> indicesToReplicate);
    }

    [ContractClassFor(typeof(IReplicationProvider))]
    internal abstract class ReplicationProviderContracts : IReplicationProvider
    {
        void IReplicationProvider.ReplicateDatabaseCreation(DocumentDatabase database)
        {
            Contract.Requires<ArgumentNullException>( database != null,"database");
            throw new System.NotImplementedException();
        }

        void IReplicationProvider.ReplicateDatabaseDeletion(string databaseName)
        {
            throw new System.NotImplementedException();
        }

        void IReplicationProvider.ReplicateIndices(string databaseName, IEnumerable<IndexDefinition> indicesToReplicate)
        {
            Contract.Requires<ArgumentNullException>(indicesToReplicate != null, "indicesToReplicate");
            throw new System.NotImplementedException();
        }
    }
}