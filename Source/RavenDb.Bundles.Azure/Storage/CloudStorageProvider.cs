﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using NLog;
using RavenDb.Bundles.Azure.Configuration;

namespace RavenDb.Bundles.Azure.Storage
{
    [Export(typeof(IStorageProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CloudStorageProvider : IStorageProvider,IDisposable
    {
        private static readonly Logger          log = LogManager.GetCurrentClassLogger();

        private readonly object                 initializationLock  = new object();
        private bool                            isInitialized       = false;

        private CloudStorageAccount             cloudStorageAccount;
        private CloudBlobClient                 cloudBlobClient;
        private CloudBlobContainer              cloudBlobContainer;
        private CloudDrive                      cloudDrive;     
        private LocalResource                   localCache;

        private DirectoryInfo                   mountedDirectory;

        [Import]
        public IConfigurationProvider           ConfigurationProvider { get; set; }

        [Import]
        public IInstanceEnumerator              InstanceEnumerator { get; set; }

        [ImportMany]
        public IEnumerable<IStorageBenchmark>   Benchmarks { get; set; } 

        public void Initialize()
        {
            if (!isInitialized)
            {
                lock (initializationLock)
                {
                    if (!isInitialized)
                    {
                        OnInitialize();
                        isInitialized = true;
                    }
                }
            }
        }

        public DirectoryInfo    GetDirectoryForDatabase(string databaseName)
        {
            if (mountedDirectory == null)
            {
                throw new InvalidOperationException("Storage provider was not initialized correctly");
            }

            var subDirectoryPath = string.IsNullOrWhiteSpace(databaseName) ? "Data" : Path.Combine("Tenants", databaseName);
            var path             = Path.Combine(mountedDirectory.FullName, subDirectoryPath);
            
            return !Directory.Exists(path) ? Directory.CreateDirectory(path) : new DirectoryInfo(path);
        }

        public void Dispose()
        {
            cloudDrive.Unmount();
        }

        private void OnInitialize()
        {
            var selfInstance        = InstanceEnumerator.GetSelf();

            cloudStorageAccount     = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(ConfigurationSettingsKeys.StorageConnectionString));
            log.Info("Storage account selected: {0}",cloudStorageAccount.BlobEndpoint);
            
            cloudBlobClient         = cloudStorageAccount.CreateCloudBlobClient();
            log.Info("Storage client created");

            var containerName       = ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.StorageContainerName,"ravendb");

            // In order to force a connection we just enumerate all available containers:
            var availableContainers = cloudBlobClient.ListContainers().ToArray();

            foreach (var container in availableContainers)
            {
                log.Info("Available container: {0}",container.Name);
            }

            if (!availableContainers.Any(c => c.Name.Equals(containerName)))
            {
                log.Info("Container {0} does not exist, creating",containerName);

                // Container does not exist:
                cloudBlobClient.GetContainerReference(containerName).Create();
            }

            cloudBlobContainer      = cloudBlobClient.GetContainerReference(containerName);
            log.Info("Container {0} selected",cloudBlobContainer.Name);

            localCache              = RoleEnvironment.GetLocalResource(ConfigurationSettingsKeys.StorageCacheResource);
            log.Info("Cache resource retrieved: {0}, path: {1}",localCache.Name,localCache.RootPath);
            CloudDrive.InitializeCache(localCache.RootPath, localCache.MaximumSizeInMegabytes);
            log.Info("Cache initialized: {0} mb",localCache.MaximumSizeInMegabytes);

            var driveName           = string.Format("{0}{1}.vhd", GetType().Namespace.Replace(".",string.Empty), selfInstance.InstanceIndex).ToLowerInvariant();
            log.Info("Virtual drive name: {0}",driveName);
            
            var pageBlob            = cloudBlobContainer.GetPageBlobReference(driveName);
            log.Info("Virtual drive blob: {0}",pageBlob.Uri);

            cloudDrive              = cloudStorageAccount.CreateCloudDrive(pageBlob.Uri.ToString());
            log.Info("Virtual drive created: {0}",cloudDrive.Uri);

            var storageSize         = ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.StorageSizeInMb, 50000);
            log.Info("Storage size: {0} mb",storageSize);

            cloudDrive.CreateIfNotExist(storageSize);
            log.Info("Virtual drive initialized: {0}",cloudDrive.Uri);

            var mountedDirectoryPath = cloudDrive.Mount(storageSize, DriveMountOptions.None);
            log.Info("Virtual drive mounted at: {0}",mountedDirectoryPath);

            mountedDirectory = new DirectoryInfo(mountedDirectoryPath);

            log.Info("Executing drive benchmark for: {0}",mountedDirectoryPath);
            ExecuteBenchmarks(mountedDirectory,storageSize);
            log.Info("Storage initialization succeeded");
        }

        private void ExecuteBenchmarks( DirectoryInfo benchmarkDirectory,int storageSize )
        {
            var benchmarkSize = ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.StorageBenchmarkSizeInMb, 1024);

            if (benchmarkSize > storageSize)
            {
                benchmarkSize = storageSize;
            }

            var averageMbsPerSecond = 0.0;

            foreach (var benchmark in Benchmarks)
            {
                var result          = benchmark.Execute(benchmarkDirectory, benchmarkSize);
                averageMbsPerSecond += result;

                log.Info("Storage benchmark {0}: {1} mb/sec",benchmark.GetType().Name.Replace("Benchmark",string.Empty),result);
            }

            if (Benchmarks.Count() > 1)
            {
                log.Info("Storage benchmark average: {0} mb/sec",averageMbsPerSecond / Benchmarks.Count());
            }
        }
    }
}
