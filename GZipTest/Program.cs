using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

//compress "d:\downloads\The Avengers.mkv" "c:\tmp\avgs.compressed"
//decompress "c:\tmp\avgrs.compressed"  "D:\Downloads\The Avengers decompressed.mkv" 

namespace GZipTest
{
    static class Program
    {
        private const int SuccessAppExitCode = 0;
        private const int ErrorAppExitCode = 1;

        [STAThread]
        private static int Main(string[] args)
        {
            CompressionMode mode;
            string srcFileName;
            string dstFileName;
            if (!ValidateStartupAgrs(args, out mode, out srcFileName, out dstFileName))
            {
                Console.WriteLine("The syntax of the command params is incorrect. Showld be: (compress|decompress) Source Destination" );
                return ErrorAppExitCode;
            }
            
            var compression = new Compression();
            compression.ProgressChanged += (_,__) => Console.Write("░");

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                compression.Cancel();
                Console.WriteLine("\nCancelling...");
            };

            try
            {
                using (var srcFile = new FileStream(srcFileName, FileMode.Open, FileAccess.Read))
                {
                    using (var dstFile = new FileStream(dstFileName, FileMode.Create, FileAccess.Write))
                    {
                        switch (mode)
                        {
                            case CompressionMode.Compress:
                                //compression.CompressAsaiwa(srcFile, dstFile);
                                compression.Compress(srcFile, dstFile);
                                break;
                            case CompressionMode.Decompress:
                                //compression.DecompressAsaiwa(srcFile, dstFile);
                                compression.Decompress(srcFile, dstFile);
                                break;
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine("\nDone.");
            }
            catch (Exception e)
            {
                File.Delete(dstFileName);
                // Не уверен, хорошая это идея или нет. 
                // Вероятно, правильный вариант -- складывать результат во временный файл рядом, 
                // а после удачного выполнения заменять уже существующий файл с таким имененем (если он есть) свежесозданным.
                // В случае неудачи старый файл окажется целым.

                if (!(e is OperationCanceledException))
                {
                    var errorLogFile = WriteErrorLog(e, dstFileName); 
                    Console.WriteLine(e.Message + Environment.NewLine + "For details please see " + errorLogFile);
                    // в это место на диске можно писать (если запрет не стал причиной экспешена) и, наверное, 
                    // пользователь именно там будет искать результат, и найдет описание причины неудачи
                }

                return ErrorAppExitCode;
            }
            
            return SuccessAppExitCode;
        }

        private static string WriteErrorLog(Exception error, string dstDataFileName)
        {
            var errorLogFile = Path.ChangeExtension(dstDataFileName, "errorLog.txt");
            File.WriteAllText(errorLogFile, error.ToString());
            return errorLogFile;
        }

        private static bool ValidateStartupAgrs(IList<string> args, out CompressionMode mode, out string srcFileName, out string dstFileName)
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
