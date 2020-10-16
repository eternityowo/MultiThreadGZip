using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadGzip.Packer.Core
{
    internal class DecompressorMultithread : GZipMultithread
    {
        int counter = 0;

        public DecompressorMultithread(string input, string output) : base(input, output)
        {
        }

        public override void Start()
        {
            Console.WriteLine("Decompressing...\n");

            Thread reader = new Thread(new ThreadStart(Read));
            reader.Start();

            for (int i = 0; i < threadCount; i++)
            {
                manualEvents[i] = new ManualResetEvent(false);

                // closure
                var ii = i;
                Thread decompressProcess = new Thread(() => Decompress(ii));

                decompressProcess.Start();
            }

            Thread writer = new Thread(new ThreadStart(Write));
            writer.Start();

            WaitHandle.WaitAll(manualEvents);

            queueWriter.Stop();

            if (!cancelled)
            {
                Console.WriteLine("\nDecompressing has been succesfully finished");
                success = true;
            }
        }

        private void Read()
        {
            try
            {
                using (FileStream compressedFile = new FileStream(sourceFile, FileMode.Open))
                {
                    while (compressedFile.Position < compressedFile.Length)
                    {
                        if (queueReader.Count() > maxQueueSize || queueWriter.Count() > maxQueueSize)
                        {
                            continue;
                        }

                        byte[] lengthBuffer = new byte[8];
                        compressedFile.Read(lengthBuffer, 0, lengthBuffer.Length);
                        int chunkLength = BitConverter.ToInt32(lengthBuffer, 4);
                        byte[] compressedData = new byte[chunkLength];
                        lengthBuffer.CopyTo(compressedData, 0);

                        compressedFile.Read(compressedData, 8, chunkLength - 8);
                        int dataSize = BitConverter.ToInt32(compressedData, chunkLength - 4);
                        byte[] lastBuffer = new byte[dataSize];

                        DataChunk chunk = new DataChunk(counter, lastBuffer, compressedData);
                        queueReader.EnqueueForWrite(chunk);
                        counter++;

                        ConsoleProgress.ProgressBar(compressedFile.Position, compressedFile.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                cancelled = true;
            }
            finally
            {
                queueReader.Stop();
            }
        }

        private void Decompress(object i)
        {
            ManualResetEvent doneEvent = manualEvents[(int)i];
            try
            {
                while (true && !cancelled)
                {
                    DataChunk chunk = queueReader.Dequeue();

                    if (chunk == null)
                    {
                        break;
                    }

                    using (MemoryStream ms = new MemoryStream(chunk.CompressedBuffer))
                    {
                        using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                        {
                            gz.Read(chunk.Buffer, 0, chunk.Buffer.Length);
                            byte[] decompressedData = chunk.Buffer.ToArray();
                            DataChunk outChunk = new DataChunk(chunk.Id, decompressedData);
                            queueWriter.EnqueueForWrite(outChunk);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in decompress thread number {i}. \n Error description: {ex.Message}");
                cancelled = true;
            }
            finally
            {
                doneEvent.Set();
            }
        }

        private void Write()
        {
            try
            {
                using (FileStream fsDecompress = new FileStream(destinationFile, FileMode.Append))
                {
                    while (true && !cancelled)
                    {
                        DataChunk chunk = queueWriter.Dequeue();

                        if (chunk == null)
                        {
                            break;
                        }

                        fsDecompress.Write(chunk.Buffer, 0, chunk.Buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                cancelled = true;
            }
        }
    }
}
