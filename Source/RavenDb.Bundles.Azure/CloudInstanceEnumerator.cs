using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
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

            var endpointConfigurationSettingsKey = roleInstance.Role.Name + "." + endpointName;

            try
            {
                // This is very nasty but apparently there is no other way as of Azure SDK 1.5 
                return RoleEnvironment.GetConfigurationSettingValue(roleInstance.Role.Name + "." + endpointName);
            }
            catch (RoleEnvironmentException ex)
            {
                throw new InvalidOperationException(string.Format("Unable to read endpoint url from configuration, endpoint: {0}, key: {1}, role: {2}, current role: {3}",endpointName,endpointConfigurationSettingsKey,roleInstance.Role.Name,RoleEnvironment.CurrentRoleInstance.Role.Name),ex);
            }
        }

        private static string EndpointToUrl( RoleInstanceEndpoint endpoint )
        {
            return string.Format("{0}://{1}:{2}", endpoint.Protocol, endpoint.IPEndpoint.Address, endpoint.IPEndpoint.Port);
        }
    }
}
