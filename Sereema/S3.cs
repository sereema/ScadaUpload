﻿using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Sereema
{
    public class S3
    {
        public const string AwsRegion = "eu-central-1";
        public const string AwsBucket = "sereema-share";
        private const string SignedHeader = "host;x-amz-content-sha256;x-amz-date";

        // Reference: http://docs.aws.amazon.com/general/latest/gr/sigv4_signing.html
        public static void UploadFile(
            string remotePath, byte[] fileContent,
            string accessKeyId, string secretAccessKey)
        {
            if (!remotePath.StartsWith("/"))
                remotePath = $"/{remotePath}";
            var hostname = $"{AwsBucket}.s3-{AwsRegion}.amazonaws.com";
            var contentHash = Sha256Hash(fileContent);
            var requestTime = DateTime.UtcNow;
            var requestTimestamp = requestTime.ToString("yyyyMMdd'T'HHmmss'Z'");
            var canonicalRequest = string.Format(
                "PUT\n{0}\n\nhost:{1}\nx-amz-content-sha256:{2}\nx-amz-date:{3}\n\n{4}\n{2}",
                Uri.EscapeUriString(remotePath), hostname, contentHash, requestTimestamp, SignedHeader);
            var scope = $"{requestTime.ToString("yyyyMMdd")}/{AwsRegion}/s3/aws4_request";
            var stringToSign =
                $"AWS4-HMAC-SHA256\n{requestTimestamp}\n{scope}\n{Sha256Hash(Encoding.UTF8.GetBytes(canonicalRequest))}";
            var signingKey = Encoding.UTF8.GetBytes($"AWS4{secretAccessKey}");
            foreach (var scopeSegment in scope.Split('/'))
                signingKey = HmacSha256(signingKey, scopeSegment);
            var signature = Hex(HmacSha256(signingKey, stringToSign));
            var requestUri = new Uri(new Uri($"https://{hostname}"), remotePath);
            var webClient = new WebClient
            {
                Headers =
                {
                    ["authorization"] =
                        $"AWS4-HMAC-SHA256 Credential={accessKeyId}/{scope},SignedHeaders={SignedHeader},Signature={signature}",
                    ["x-amz-content-sha256"] = contentHash,
                    ["x-amz-date"] = requestTimestamp
                }
            };
            webClient.UploadData(requestUri, "PUT", fileContent);
        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            return new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static string Sha256Hash(byte[] data)
        {
            return Hex(SHA256.Create().ComputeHash(data));
        }

        private static string Hex(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "").ToLower();
        }
    }
}