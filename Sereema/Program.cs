using System;
using System.IO;

namespace Sereema
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 5)
            {
                S3.UploadFile(
                    args[1].Replace("\\", "/"),
                    File.ReadAllBytes(args[0]),
                    args[2], args[3], args[4]);
            }
            else
            {
                Console.WriteLine("Usage: Sereema.exe local_filepath remote_filepath username access_key_id secret_access_key");
                Environment.Exit(-1);
            }
        }
    }
}