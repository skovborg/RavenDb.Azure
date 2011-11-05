using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace RavenDb.Bundles.Azure.Storage
{
    [ContractClass(typeof(StorageBenchmarkContracts))]
    public interface IStorageBenchmark
    {
        double Execute(DirectoryInfo targetDirectory,int sizeHintInMb);
    }

    [ContractClassFor(typeof(IStorageBenchmark))]
    internal abstract class StorageBenchmarkContracts : IStorageBenchmark
    {
        double IStorageBenchmark.Execute(DirectoryInfo targetDirectory,int sizeHintInMb)
        {
            Contract.Requires<ArgumentNullException>( targetDirectory != null,"targetDirectory");
            Contract.Requires<ArgumentOutOfRangeException>( sizeHintInMb >= 1,"sizeHintInMb");

            throw new System.NotImplementedException();
        }
    }
}