using System;
using System.IO;

namespace Sereema
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 4)
            {
                S3.UploadFile(
                    Path.GetFileName(args[0]),
                    File.ReadAllBytes(args[0]),
                    args[1], args[2], args[3]);
            }
            else
            {
                Console.WriteLine("Usage: Sereema.exe filepath username access_key_id secret_access_key");
                Environment.Exit(-1);
            }
        }
    }
}