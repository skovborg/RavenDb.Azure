using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RavenDb.Bundles.Azure.Replication.Recipes
{
    [Export(typeof(IReplicationRecipe))]
    public class SeperateReadersAndWritersRecipe : IReplicationRecipe
    {
        public IEnumerable<InstanceDescription> GetReplicationTargets(InstanceDescription self, IEnumerable<InstanceDescription> otherInstances)
        {
            if (self.InstanceType == InstanceType.ReadWrite)
            {
                return self.InstanceIndex == 0 ? otherInstances : otherInstances.Where(i => i.InstanceType == InstanceType.ReadWrite && i.InstanceIndex == 0);
            }

            return null;
        }
    }
}
