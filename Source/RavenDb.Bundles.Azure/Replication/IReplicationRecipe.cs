using System.Collections.Generic;

namespace RavenDb.Bundles.Azure.Replication
{
    public interface IReplicationRecipe
    {
        IEnumerable<InstanceDescription> GetReplicationTargets(InstanceDescription self, IEnumerable<InstanceDescription> otherInstances);
    }
}