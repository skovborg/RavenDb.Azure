using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;

namespace RavenDb.Bundles.Azure.Storage.Benchmarks
{
    [Export(typeof(IStorageBenchmark))]
    public class RandomReadBenchmark : StorageBenchmarkBase
    {
        private const int ReadSize = 4096;
        private const int ReductionFactor = 1;

        private List<Tuple<int>> readEntries; 

        protected override void OnPrepare(System.IO.FileStream targetStream, long sizeHint)
        {
            sizeHint = sizeHint/ReductionFactor;

            FillFile(targetStream, sizeHint);

            var numberReads = sizeHint / ReadSize;
            var lastReadSize = sizeHint % ReadSize;

            readEntries = Enumerable.Range(0, (int)numberReads).Select(i => Tuple.Create(ReadSize)).ToList();
            if (lastReadSize > 0)
            {
                readEntries.Add(Tuple.Create((int)lastReadSize));
            }
        }

        protected override long OnExecute(System.IO.FileStream targetStream, long sizeHint)
        {
            sizeHint = sizeHint/ReductionFactor;

            var readBuffer  = new byte[ReadSize];
            var randomizer  = new Random();

            while (readEntries.Any())
            {
                var index       = randomizer.Next(0, readEntries.Count);
                var readEntry   = readEntries[index];

                targetStream.Seek((long) index*(long) ReadSize, SeekOrigin.Begin);
                targetStream.Read(readBuffer, 0, readEntry.Item1);

                readEntries.RemoveAt(index);
            }

            return sizeHint;
        }
    }
}
