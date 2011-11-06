using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RavenDb.Bundles.Azure.Replication.Recipes
{
    [Export(typeof(IReplicationRecipe))]
    public class PeerToPeerReplicationRecipe : IReplicationRecipe
    {
        public IEnumerable<InstanceDescription> GetReplicationTargets(InstanceDescription self, IEnumerable<InstanceDescription> otherInstances)
        {
            return otherInstances;
        }
    }
}
