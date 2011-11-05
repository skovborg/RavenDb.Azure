using System;
using System.Diagnostics.Contracts;

namespace RavenDb.Bundles.Azure.Configuration
{
    [ContractClass(typeof(ConfigurationProviderContracts))]
    public interface IConfigurationProvider
    {
        string GetSetting(string key);
    }

    [ContractClassFor(typeof(IConfigurationProvider))]
    internal abstract class ConfigurationProviderContracts : IConfigurationProvider
    {
        string IConfigurationProvider.GetSetting(string key)
        {
            Contract.Requires<ArgumentException>( !string.IsNullOrWhiteSpace(key),"key");

            throw new NotImplementedException();
        }
    }

    public static class ConfigurationProviderExtensions
    {
        public static TValue GetSetting<TValue>(this IConfigurationProvider configurationProvider, string key,TValue defaultValue)
        {
            Contract.Requires<ArgumentNullException>( configurationProvider != null,"configurationProvider");
       
            var rawValue = configurationProvider.GetSetting(key);

            if (rawValue != null)
            {
                return (TValue) Convert.ChangeType(rawValue, typeof (TValue));
            }

            return defaultValue;
        }
    }
}