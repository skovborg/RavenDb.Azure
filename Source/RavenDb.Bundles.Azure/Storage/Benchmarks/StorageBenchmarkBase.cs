using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RavenDb.Bundles.Azure.Storage.Benchmarks
{
    public abstract class StorageBenchmarkBase : IStorageBenchmark
    {
        public double Execute(DirectoryInfo targetDirectory, int sizeHintInMb)
        {
            double result   = 0.0;
            var file        = GetBenchmarkFile(targetDirectory);

            if (file.Exists)
            {
                file.Delete();
            }

            using (var fileStream = new FileStream(file.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var sizeInBytes = (long) sizeHintInMb*1000L*1000L;

                OnPrepare(fileStream, sizeInBytes);
                fileStream.Flush();
                fileStream.Seek(0L, SeekOrigin.Begin);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var actualSizeInBytes = OnExecute(fileStream,sizeInBytes);
                fileStream.Flush(true);

                stopwatch.Stop();
                result = ((double) actualSizeInBytes/stopwatch.Elapsed.TotalSeconds)/(1000.0*1000.0);
            }

            if (file.Exists)
            {
                file.Delete();
            }

            return result;
        }

        protected FileInfo GetBenchmarkFile(DirectoryInfo targetDirectory)
        {
            var filePath = Path.Combine(targetDirectory.FullName, GetType().Name + ".benchmark");
            return new FileInfo(filePath);
        }

        protected void FillFile(FileStream targetStream,long sizeHint)
        {
            var writeBuffer     = new byte[4096];
            var numberOfWrites  = sizeHint / writeBuffer.Length;
            var lastWriteSize   = sizeHint%writeBuffer.Length;

            for (long i = 0; i < numberOfWrites; ++i)
            {
                targetStream.Write(writeBuffer, 0, writeBuffer.Length);
            }

            if (lastWriteSize > 0)
            {
                targetStream.Write(writeBuffer,0,(int)lastWriteSize);
            }
        }

        protected virtual void OnPrepare(FileStream targetStream,long sizeHint)
        {}

        protected abstract long OnExecute(FileStream targetStream, long sizeHint);
    }
}
