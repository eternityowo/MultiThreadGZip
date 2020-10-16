using System;
using System.IO;

namespace MultithreadGzip.Check
{
    internal static class EntryPoint
    {
        public static int Main(string[] args)
        {
            var pathToProject = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../MultithreadGzip.Packer"));
            if (args.Length == 1)
                pathToProject = args[0];
            
            //var pathToGZipProject = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../Kontur.LogPackerGZip"));

            Console.WriteLine($"Running self checks on project '{pathToProject}'..");

            return new SolutionChecker(pathToProject).Run() ? 0 : 1;
        }
    }
}