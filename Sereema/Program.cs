using System;
using System.Collections.Generic;
using System.IO;

namespace Sereema
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: Sereema.exe credentials_filepath local_filepath remote_filepath");
                Environment.Exit(-1);
            }
            var credentialsFilepath = args[0];
            var localFilepath = args[1];
            // TODO: let remoteFilepath be optional and guessed from localFilepath
            var remoteFilepath = args[2];
            string remotePrefix = null;
            string accessKeyId = null;
            string secretAccessKey = null;
            foreach (var line in ReadLines(credentialsFilepath))
            {
                ExtractValueMatchingKey(line, "remote_prefix", ref remotePrefix);
                ExtractValueMatchingKey(line, "access_key_id", ref accessKeyId);
                ExtractValueMatchingKey(line, "secret_access_key", ref secretAccessKey);
            }
            if (remotePrefix == null || accessKeyId == null || secretAccessKey == null)
            {
                Console.WriteLine("Error: invalid credentials file");
                Environment.Exit(-1);
            }
            S3.UploadFile(
                $"{remotePrefix}{remoteFilepath.Replace("\\", "/")}",
                File.ReadAllBytes(localFilepath), accessKeyId, secretAccessKey);
        }

        private static IEnumerable<string> ReadLines(string filePath)
        {
            using (var streamReader = new StreamReader(filePath))
                for (var line = streamReader.ReadLine(); line != null; line = streamReader.ReadLine())
                    yield return line;
        }

        private static void ExtractValueMatchingKey(string line, string key, ref string value)
        {
            if (line.StartsWith($"{key}:"))
                value = line.Split(':')[1].Trim();
        }
    }
}