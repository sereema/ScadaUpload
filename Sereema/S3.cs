using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Sereema
{
    public class S3
    {
        public const string AwsRegion = "eu-central-1";
        public const string AwsBucket = "sereema-share";
        public const string AwsFolder = "scada";
        private const string SignedHeader = "host;x-amz-content-sha256;x-amz-date";

        // Reference: http://docs.aws.amazon.com/general/latest/gr/sigv4_signing.html
        public static void UploadFile(
            string fileName, byte[] fileContent,
            string userName, string accessKeyId, string secretAccessKey)
        {
            var requestPath = string.Format("/{0}/{1}/{2}", userName, AwsFolder, fileName);
            var hostname = string.Format("{0}.s3-{1}.amazonaws.com", AwsBucket, AwsRegion);
            var contentHash = Sha256Hash(fileContent);
            var requestTime = DateTime.UtcNow;
            var requestTimestamp = requestTime.ToString("yyyyMMdd'T'HHmmss'Z'");
            var canonicalRequest = string.Format(
                "PUT\n{0}\n\nhost:{1}\nx-amz-content-sha256:{2}\nx-amz-date:{3}\n\n{4}\n{2}",
                Uri.EscapeUriString(requestPath), hostname, contentHash, requestTimestamp, SignedHeader);
            var scope = string.Format(
                "{0}/{1}/s3/aws4_request", requestTime.ToString("yyyyMMdd"), AwsRegion);
            var stringToSign = string.Format(
                "AWS4-HMAC-SHA256\n{0}\n{1}\n{2}",
                requestTimestamp, scope,
                Sha256Hash(Encoding.UTF8.GetBytes(canonicalRequest)));
            var signingKey = Encoding.UTF8.GetBytes(string.Format("AWS4{0}", secretAccessKey));
            foreach (var scopeSegment in scope.Split('/'))
                signingKey = HmacSha256(signingKey, scopeSegment);
            var signature = Hex(HmacSha256(signingKey, stringToSign));
            var requestUri = new Uri(string.Format("https://{0}{1}", hostname, requestPath));
            var webClient = new WebClient();
            webClient.Headers["authorization"] = string.Format(
                "AWS4-HMAC-SHA256 Credential={0}/{1},SignedHeaders={2},Signature={3}",
                accessKeyId, scope, SignedHeader, signature);
            webClient.Headers["x-amz-content-sha256"] = contentHash;
            webClient.Headers["x-amz-date"] = requestTimestamp;
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