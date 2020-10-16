using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadGzip.Packer.Core
{
    internal abstract class GZipMultithread
    {
        protected bool cancelled = false;
        protected bool success = false;
        protected readonly string sourceFile, destinationFile;
        protected static int threadCount = Environment.ProcessorCount;

        protected int chunkSize = 4096;
        protected ChunkQueue queueReader = new ChunkQueue();
        protected ChunkQueue queueWriter = new ChunkQueue();
        protected ManualResetEvent[] manualEvents = new ManualResetEvent[threadCount];
        protected int maxQueueSize = threadCount * 64;

        public GZipMultithread()
        {
        }

        public GZipMultithread(string input, string output)
        {
            this.chunkSize = DiskInfo.GetClusterSize(input) * 256;
            this.sourceFile = input;
            this.destinationFile = output;
        }

        public int Result()
        {
            if (success && !cancelled)
            {
                return 0;
            }
            return 1;
        }

        public void Cancel()
        {
            cancelled = true;
        }

        public abstract void Start();
    }
}
