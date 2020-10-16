using MultithreadGzip.Packer.Core;
using System;

namespace MultithreadGzip.Packer
{
    internal class EntryPoint
    {
        static GZipMultithread zipper;
        public static int Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);

            //ShowInfo();

            //args = new string[3];
            //args[0] = @"compress";
            //args[1] = @"C:\GZip\example.log";
            //args[2] = @"C:\GZip\example.log.compressed";

            //args[0] = @"decompress";
            //args[1] = @"C:\GZip\example.log.compressed.gz";
            //args[2] = @"C:\GZip\example.log.uncompressed";

            Validate.ValidateStringArgs(args);

            try
            {

                switch (args[0].ToLower())
                {
                    case "compress":
                        zipper = new CompressorMultithread(args[1], args[2]);
                        break;
                    case "decompress":
                        zipper = new DecompressorMultithread(args[1], args[2]);
                        break;
                }

                zipper.Start();

                //Console.ReadKey();

                return zipper.Result();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error is occured!\n Method: {ex.TargetSite}\n Error description {ex.Message}");
                return 1;
            }
        }
        static void ShowInfo()
        {
            Console.WriteLine("To zip\\unzip: GZipTest.exe compress\\decompress [Source file path] [Destination file path]\n" +
                              "To stop prpgramm: use CTRL + C");
        }


        static void CancelKeyPress(object sender, ConsoleCancelEventArgs _args)
        {
            if (_args.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine("\nCancelling...");
                _args.Cancel = true;
                zipper.Cancel();
            }
        }
    }
}
