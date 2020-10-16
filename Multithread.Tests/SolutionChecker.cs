using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace MultithreadGzip.Check
{
    internal class SolutionChecker
    {
        private const string TemporaryDirectoryPath = "templog";
        private static readonly string UncompressedFile = Path.GetFullPath(Path.Combine(TemporaryDirectoryPath, "example.log"));
        private static readonly string CompressedFile = Path.GetFullPath(Path.Combine(TemporaryDirectoryPath, "example.log.compressed"));
        private static readonly string DecompressedFile = Path.GetFullPath(Path.Combine(TemporaryDirectoryPath, "example.log.decompressed"));
        
        private readonly string pathToSolutionBinary;

        public SolutionChecker(string pathToProject)
        {
            pathToSolutionBinary = new SolutionPublisher().Publish(pathToProject);
        }

        private static readonly string[] Tests =
        {
            nameof(RestoreOriginalFileAfterDecompression)
        };

        public bool Run()
        {
            var someTestsFailed = false;
            
            foreach (var test in Tests)
            {
                Console.WriteLine($"{test}:");
                try
                {
                    Directory.CreateDirectory(TemporaryDirectoryPath);
                    try
                    {
                        File.Copy("example.log", UncompressedFile);
                        GetType().GetMethod(test, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(this, null);
                    }
                    finally
                    {
                        Directory.Delete(TemporaryDirectoryPath, true);
                    }
                    Console.WriteLine("\tPassed.");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"\tFailed: {error?.InnerException?.Message ?? error.ToString()}");
                    someTestsFailed = true;
                }
                Console.WriteLine();
            }

            return !someTestsFailed;
        }

        #region Helpers

        private void Compress()
        {
            var project = pathToSolutionBinary;
            
            RunSolution(project, $"compress {UncompressedFile} {CompressedFile}");
        }
        
        private void Decompress()
        {
            var project = pathToSolutionBinary;

            RunSolution(project, $"decompress {CompressedFile + ".gz"} {DecompressedFile}");
        }

        private TimeSpan Time(Action action)
        {
            const int iterations = 20;
            
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                action();

            return watch.Elapsed.Divide(iterations);
        }

        private void RunSolution(string binaryPath, string args)
        {
            var options = new ProcessStartInfo("dotnet", $"{binaryPath} {args}")
            {
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(binaryPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            var process = Process.Start(options);
            if (process == null)
                throw new Exception("Failed to start 'dotnet'.");

            process.WaitForExit();
            
            Console.Write(process.StandardOutput.ReadToEnd());
            Console.Write(process.StandardError.ReadToEnd());
        }

        #endregion

        #region Tests

        private void RestoreOriginalFileAfterDecompression()
        {
            Compress();
            Decompress();
            
            var equals = StructuralComparisons.StructuralEqualityComparer.Equals(
                File.ReadAllBytes(UncompressedFile), File.ReadAllBytes(DecompressedFile));

            if (!equals)
                throw new Exception("File was corrupted after decompression!");
        }
        
        #endregion
    }
}