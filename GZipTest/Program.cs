 //#define ETUDE

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

// Debug command line args examples:
// 'compress "d:\downloads\The Avengers.mkv" "c:\tmp\avgs.compressed"'
// 'decompress "c:\tmp\avgrs.compressed" "D:\Downloads\The Avengers decompressed.mkv"'

namespace GZipTest
{
    internal static class Program
    {
        private const int SuccessAppExitCode = 0;
        private const int ErrorAppExitCode = 1;

        [STAThread]
        private static int Main(string[] args)
        {
            CompressionMode mode;
            string sourceFileName;
            string targetFileName;
            if (!ValidateStartupArgs(args, out mode, out sourceFileName, out targetFileName))
            {
                Console.WriteLine("The syntax of the command params is incorrect. Should be: (compress|decompress) Source Destination");
                return ErrorAppExitCode;
            }
            
            var compression = new Compression();
            compression.ProgressChanged += delegate { Console.Write("░"); };

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                compression.Cancel();
                Console.WriteLine("\nCanceling...");
            };

            try
            {
                using (var sourceFile = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read))
                {
                    using (var targetFile = new FileStream(targetFileName, FileMode.Create, FileAccess.Write))
                    {

                        switch (mode)
                        {
#if ETUDE
                            case CompressionMode.Compress:
                                compression.CompressEtude(sourceFile, targetFile);
                                break;
                            case CompressionMode.Decompress:
                                compression.DecompressEtude(sourceFile, targetFile);
                                break;
#else
                            case CompressionMode.Compress:
                                compression.Compress(sourceFile, targetFile);
                                break;
                            case CompressionMode.Decompress:
                                compression.Decompress(sourceFile, targetFile);
                                break;
#endif
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Done: "+ targetFileName);
            }
            catch (Exception e)
            {
                File.Delete(targetFileName);

                if (e is OperationCanceledException)
                    return ErrorAppExitCode;

                Console.WriteLine(e.Message);

                var errorLogFile = WriteErrorLog(e, targetFileName); 

                Console.WriteLine("For details please see " + errorLogFile);

                return ErrorAppExitCode;
            }
            
            return SuccessAppExitCode;
        }

        private static string WriteErrorLog(Exception error, string compressionDestinationFileName)
        {
            var errorLogFile = Path.ChangeExtension(compressionDestinationFileName, "errorLog.txt");
            File.WriteAllText(errorLogFile ?? "null", error.ToString());
            return errorLogFile;
        }

        private static bool ValidateStartupArgs(IList<string> args, out CompressionMode mode, out string srcFileName, out string dstFileName)
        {
            mode = default (CompressionMode);
            srcFileName = null;
            dstFileName = null;

            if (args.Count < 3)
                return false;

            switch (args[0])
            {
                case "compress": mode = CompressionMode.Compress;
                    break;
                case "decompress": mode = CompressionMode.Decompress;
                    break;
                default:
                    return false;
            }

            srcFileName = args[1];
            dstFileName = args[2];
            return true;
        }
    }
}
