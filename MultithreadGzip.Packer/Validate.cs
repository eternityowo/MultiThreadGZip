using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadGzip.Packer
{
    internal static class Validate
    {
        public static void ValidateStringArgs(string[] args)
        {

            if (args.Length == 0 || args.Length > 3 || args.Any(x => string.IsNullOrEmpty(x)))
            {
                throw new Exception("Please enter arguments up to the following pattern:\n compress(decompress) [Source file] [Destination file].");
            }

            if (args[0].ToLower() != "compress" && args[0].ToLower() != "decompress")
            {
                throw new Exception("First argument shall be \"compress\" or \"decompress\".");
            }

            if (args[1].Length == 0)
            {
                throw new Exception("No source file name was specified.");
            }

            if (!File.Exists(args[1]))
            {
                throw new Exception("No source file was found.");
            }

            FileInfo sourceFile = new FileInfo(args[1]);
            FileInfo destinationFile = new FileInfo(args[2]);

            if (args[1] == args[2])
            {
                throw new Exception("Source and destination files shall be different.");
            }

            if (sourceFile.Extension == ".gz" && args[0] == "compress")
            {
                throw new Exception("File has already been compressed.");
            }

            if (destinationFile.Extension == ".gz" && destinationFile.Exists)
            {
                throw new Exception("Destination file already exists. Please indiciate the different file name.");
            }

            if (sourceFile.Extension != ".gz" && args[0] == "decompress")
            {
                throw new Exception("File to be decompressed shall have .gz extension.");
            }

            if (args[2].Length == 0)
            {
                throw new Exception("No destination file name was specified.");
            }
        }
    }
}
