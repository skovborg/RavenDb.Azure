﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace RavenDb.Bundles.Azure
{
    [Export(typeof(IInstanceEnumerator))]
    public class CloudInstanceEnumerator : IInstanceEnumerator
    {
        public IEnumerable<InstanceDescription> EnumerateInstances()
        {
            var instances = RoleEnvironment.Roles.SelectMany(r => r.Value.Instances);

            return instances.Select(i => new InstanceDescription()
            {
                Id                  = i.Id,
                ExternalUrl         = GetEndpointUrl(i,"PublicHttpEndpoint"),
                InternalUrl         = GetEndpointUrl(i,"PrivateHttpEndpoint"),
                RoleName            = i.Role.Name,
                RoleInstanceIndex   = int.Parse(i.Id.Substring(i.Id.LastIndexOf('_') + 1)),
                IsSelf              = i.Id.Equals(RoleEnvironment.CurrentRoleInstance.Id, StringComparison.OrdinalIgnoreCase),
                FriendlyName        = i.Id.Replace("-", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty).Replace("(", string.Empty).Replace(")", String.Empty).ToLowerInvariant()
            });
        }

        private static string GetEndpointUrl( RoleInstance roleInstance,string endpointName )
        {
            RoleInstanceEndpoint endpoint = null;

            if (roleInstance.InstanceEndpoints.TryGetValue(endpointName, out endpoint))
            {
                return EndpointToUrl(endpoint);
            }

            // This is very nasty but apparently there is no other way as of Azure SDK 1.5 
            return RoleEnvironment.GetConfigurationSettingValue(roleInstance.Role.Name + "." + endpoint);
        }

        private static string EndpointToUrl( RoleInstanceEndpoint endpoint )
        {
            return string.Format("{0}://{1}:{2}", endpoint.Protocol, endpoint.IPEndpoint.Address, endpoint.IPEndpoint.Port);
        }
    }
}
