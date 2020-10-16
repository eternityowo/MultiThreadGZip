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
    internal class CompressorMultithread : GZipMultithread
    {
        public CompressorMultithread(string input, string output) : base(input, output)
        {

        }

        public override void Start()
        {
            Console.WriteLine("Compressing...\n");

            Thread reader = new Thread(new ThreadStart(Read));
            reader.Start();

            for (int i = 0; i < threadCount; i++)
            {
                manualEvents[i] = new ManualResetEvent(false);

                // closure
                var ii = i;
                Thread compressProcess = new Thread(() => Compress(ii));

                compressProcess.Start();
            }

            Thread writer = new Thread(new ThreadStart(Write));
            writer.Start();

            WaitHandle.WaitAll(manualEvents);

            queueWriter.Stop();

            if (!cancelled)
            {
                Console.WriteLine("\nCompressing has been succesfully finished");
                success = true;
            }
        }

        private void Read()
        {
            try
            {
                using (FileStream sourceFile = new FileStream(base.sourceFile, FileMode.Open))
                {
                    int bytesRead;
                    byte[] lastBuffer;
                    long fileLength = sourceFile.Length;

                    while (sourceFile.Position < fileLength && !cancelled)
                    {
                        if(queueReader.Count() > maxQueueSize || queueWriter.Count() > maxQueueSize)
                        {
                            continue;
                        }

                        if (fileLength - sourceFile.Position <= chunkSize)
                        {
                            bytesRead = (int)(fileLength - sourceFile.Position);
                        }
                        else
                        {
                            bytesRead = chunkSize;
                        }

                        lastBuffer = new byte[bytesRead];
                        sourceFile.Read(lastBuffer, 0, bytesRead);
                        queueReader.EnqueueForCompress(lastBuffer);

                        ConsoleProgress.ProgressBar(sourceFile.Position, sourceFile.Length);
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

        private void Compress(object i)
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

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (GZipStream cs = new GZipStream(ms, CompressionMode.Compress))
                        {
                            cs.Write(chunk.Buffer, 0, chunk.Buffer.Length);
                        }

                        byte[] compressedData = ms.ToArray();
                        DataChunk outChunk = new DataChunk(chunk.Id, compressedData);
                        queueWriter.EnqueueForWrite(outChunk);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in compressor thread number {i}. \n Error description: {ex.Message}");
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
                using (FileStream fsCompress = new FileStream(destinationFile + ".gz", FileMode.Append))
                {
                    while (true && !cancelled)
                    {
                        DataChunk chunk = queueWriter.Dequeue();

                        if (chunk == null)
                        {
                            break;
                        }

                        BitConverter.GetBytes(chunk.Buffer.Length).CopyTo(chunk.Buffer, 4);
                        fsCompress.Write(chunk.Buffer, 0, chunk.Buffer.Length);
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
