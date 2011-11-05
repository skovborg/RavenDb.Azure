using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RavenDb.Bundles.Azure.Storage.Benchmarks
{
    [Export(typeof(IStorageBenchmark))]
    public class SequentialReadBenchmark : StorageBenchmarkBase
    {
        protected override void OnPrepare(System.IO.FileStream targetStream, long sizeHint)
        {
            FillFile(targetStream,sizeHint);
        }

        protected override long OnExecute(System.IO.FileStream targetStream, long sizeHint)
        {
            var readBuffer      = new byte[4096];
            var numberReads     = sizeHint / readBuffer.Length;
            var lastReadSize    = sizeHint % readBuffer.Length;

            for (long i = 0; i < numberReads; ++i)
            {
                targetStream.Read(readBuffer, 0, readBuffer.Length);
            }

            if (lastReadSize > 0)
            {
                targetStream.Read(readBuffer, 0, (int)lastReadSize);
            }

            return sizeHint;
        }
    }
}
