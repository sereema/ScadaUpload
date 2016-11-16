using System;
using System.Collections.Generic;
using System.IO;

namespace Sereema
{
    internal class Program
    {
        private const int ErrorBadArguments = 0xA0;
        private const int ErrorInvalidCommandLine = 0x667;

        private static void Main(string[] args)
        {
            var programName = AppDomain.CurrentDomain.FriendlyName;
            if (args.Length != 2 && args.Length != 3)
                Fail(
                    $"Usage: {programName} credentials_filepath local_filepath [remote_filepath]",
                    ErrorInvalidCommandLine);
            var credentialsFilepath = args[0];
            var localFilepath = args[1];
            var remoteFilepath = args.Length >= 3 ? args[2] : Path.GetFileName(args[1]);
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
                Fail("Error: invalid credentials file", ErrorBadArguments);
            S3.UploadFile(
                // ReSharper disable once PossibleNullReferenceException
                $"{remotePrefix}{remoteFilepath.Replace("\\", "/")}",
                File.ReadAllBytes(localFilepath), accessKeyId, secretAccessKey);
        }

        private static void Fail(string message, int exitCode)
        {
            Console.WriteLine(message);
            Environment.Exit(exitCode);
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