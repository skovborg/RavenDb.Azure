using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Raven.Abstractions.Replication;

namespace RavenDb.Bundles.Azure
{
    public class InstanceDescription
    {
        public string       Id                  { get; set; }
        public string       RoleName            { get; set; }
        public int          RoleInstanceIndex   { get; set; }
        public string       FriendlyName        { get; set; }
        public string       ExternalUrl         { get; set; }
        public string       InternalUrl         { get; set; }
        public bool         IsSelf              { get; set; }
    }

    public interface IInstanceEnumerator
    {
        IEnumerable<InstanceDescription> EnumerateInstances();
    }

    public static class InstanceEnumeratorExtensions
    {
        public static InstanceDescription GetSelf( this IInstanceEnumerator instanceEnumerator )
        {
            Contract.Requires<ArgumentNullException>( instanceEnumerator != null,"instanceEnumerator");

            return instanceEnumerator.EnumerateInstances().First(i => i.IsSelf);
        }

        public static IEnumerable<InstanceDescription> GetOthers( this IInstanceEnumerator instanceEnumerator )
        {
            return instanceEnumerator.EnumerateInstances().Where(i => !i.IsSelf);
        }
    }
}