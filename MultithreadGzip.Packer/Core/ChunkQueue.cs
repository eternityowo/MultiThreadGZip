using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadGzip.Packer.Core
{
    public class DataChunk
    {
        public int Id { get; }
        public byte[] Buffer { get; }
        public byte[] CompressedBuffer { get; }

        public DataChunk(int id, byte[] buffer) : this(id, buffer, new byte[0])
        {
        }

        public DataChunk(int id, byte[] buffer, byte[] compressedBuffer)
        {
            this.Id = id;
            this.Buffer = buffer;
            this.CompressedBuffer = compressedBuffer;
        }
    }

    public class ChunkQueue
    {
        private object _locker = new object();
        private Queue<DataChunk> _queue = new Queue<DataChunk>();
        private bool _isAlive = true;
        private int _currentId = 0;

        public int Count()
        {
            lock (_locker)
            {
                return _queue.Count;
            }
        }

        public void EnqueueForWrite(DataChunk chunk)
        {
            int id = chunk.Id;
            lock (_locker)
            {
                if (!_isAlive)
                {
                    throw new InvalidOperationException("Queue already stopped");
                }

                while (id != _currentId)
                {
                    Monitor.Wait(_locker);
                }

                _queue.Enqueue(chunk);
                _currentId++;
                Monitor.PulseAll(_locker);
            }
        }

        public void EnqueueForCompress(byte[] buffer)
        {
            lock (_locker)
            {
                if (!_isAlive)
                {
                    throw new InvalidOperationException("Queue already stopped");
                }

                DataChunk chunk = new DataChunk(_currentId, buffer);
                _queue.Enqueue(chunk);

                _currentId++;

                Monitor.PulseAll(_locker);
            }
        }

        public DataChunk Dequeue()
        {
            lock (_locker)
            {
                while (_queue.Count == 0 && _isAlive)
                {
                    Monitor.Wait(_locker);
                }

                if (_queue.Count == 0)
                    return null;

                return _queue.Dequeue();

            }
        }

        public void Stop()
        {
            lock (_locker)
            {
                _isAlive = false;
                Monitor.PulseAll(_locker);
            }
        }
    }
}
