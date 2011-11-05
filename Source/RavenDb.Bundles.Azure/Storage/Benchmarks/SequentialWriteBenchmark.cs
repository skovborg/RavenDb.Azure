using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RavenDb.Bundles.Azure.Storage.Benchmarks
{
    [Export(typeof(IStorageBenchmark))]
    public class SequentialWriteBenchmark : StorageBenchmarkBase
    {
        protected override long OnExecute(System.IO.FileStream targetStream, long sizeHint)
        {
            FillFile(targetStream,sizeHint);

            return sizeHint;
        }
    }
}
