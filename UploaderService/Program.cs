using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.DependencyInjection;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    public static List<byte[]> generatedBytes = new List<byte[]>();
    public static ConcurrentBag<Task> uploadTasks = new ConcurrentBag<Task>();
    public static SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(60),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(20),
        MaxConnectionsPerServer = 1000
    };
    public static HttpClient httpClient = new HttpClient(socketsHttpHandler);

    static async Task Main(string[] args)
    {
        Byte[] bytes = new Byte[256];
        string data = null;

        var ipEndPoint = new IPEndPoint(IPAddress.Any, 1337);
        TcpListener listener = new(ipEndPoint);

        try
        {
            listener.Start();

            using TcpClient handler = await listener.AcceptTcpClientAsync();
            await using NetworkStream stream = handler.GetStream();

            int i;

            // Loop to receive all the data sent by the client.
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                Console.WriteLine("Received: {0}", data);

                // Process the data sent by the client.
                data = data.ToUpper();

                byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                // Send back a response.
                stream.Write(msg, 0, msg.Length);
                Console.WriteLine("Sent: {0}", data);
            }
        }
        finally
        {
            listener.Stop();
        }

        Console.ReadKey();
        listener.Stop();
        return;

        string accessKey = "oDvlANSpdkreqwpo";
        string secretKey = "f5Zhdxyys8fO2ye8mvjBrnm3skgts3gtgaImIseX";
        string bucketName = "dev2";
        int numFiles = 1000;
        int minFileSize = 1024; // 1KB
        int maxFileSize = 102400; // 100KB

        AmazonS3Config cfg = new AmazonS3Config { ServiceURL = "https://s3.tebi.io" };
        AmazonS3Client s3Client = new AmazonS3Client(accessKey, secretKey, cfg);

        uploadTasks.Add(Monitor());

        string url = $"https://{bucketName}.s3.tebi.io/";

        httpClient.DefaultRequestHeaders.Add("Authorization", $"TB-PLAIN {accessKey}:{secretKey}");

        while (true)
        {
            var watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();

            //var client = httpClientFactory.CreateClient("tabi");


            Parallel.ForEach(
                Enumerable.Range(0, numFiles),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                number =>
                {
                    string key = $"file{number}_{Guid.NewGuid()}.dat";
                    byte[] fileBytes = GenerateRandomFile(minFileSize, maxFileSize);
                    var content = new StreamContent(new MemoryStream(fileBytes));
                    Task uploadTask = httpClient.PutAsync($"https://{bucketName}.s3.tebi.io/{key}", content);
                    uploadTasks.Add(uploadTask);
                });

            //await Task.WhenAll(uploadTasks);

            watch2.Stop();

            if (1000 - watch2.ElapsedMilliseconds > 0)
            {
                await Task.Delay(1000 - (int)watch2.ElapsedMilliseconds);
            }

            //Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
        }
        

        //Console.WriteLine("All files uploaded to S3.");
    }

    public static async Task Monitor()
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        while (true)
        {
            await Task.Delay(1000);

            var countCompleted = uploadTasks.Count(x => x.IsCompletedSuccessfully);
            var countAll = uploadTasks.Count() - 1;

            var cfps = countCompleted / (watch.ElapsedMilliseconds / 1000);
            var fps = countAll / (watch.ElapsedMilliseconds / 1000);

            Console.WriteLine($"{watch.ElapsedMilliseconds} ms! {fps} requests per second / {cfps} completed files per second");
        }
    }

    static byte[] GenerateRandomFile(int minSize, int maxSize)
    {
        Random random = new Random();
        int fileSize = random.Next(minSize, maxSize + 1);
        byte[] fileBytes = new byte[fileSize];
        random.NextBytes(fileBytes);
        return fileBytes;
    }

    static string OneTimeHmac(string key, string secretKey, string  date, string region, string bucketName, string objectKey)
    {
        string amzDate = date.Substring(0, 8);

        string canonicalRequest = $"PUT\n/{objectKey}\n\nhost:{bucketName}.s3.amazonaws.com\nx-amz-date:{date}\n\nhost;x-amz-date\nUNSIGNED-PAYLOAD";
        string hashedCanonicalRequest = HashSHA256(canonicalRequest);

        string stringToSign = $"AWS4-HMAC-SHA256\n{date}\n{amzDate}/{region}/s3/aws4_request\n{hashedCanonicalRequest}";
        byte[] signingKey = GetSignatureKey(secretKey, amzDate, region, "s3");

        byte[] signature = HashHMACSHA256(stringToSign, signingKey);

        return BitConverter.ToString(signature).Replace("-", "").ToLower();
    }
    static string HashSHA256(string text)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        byte[] kDate = HashHMACSHA256(dateStamp, Encoding.UTF8.GetBytes("AWS4" + key));
        byte[] kRegion = HashHMACSHA256(regionName, kDate);
        byte[] kService = HashHMACSHA256(serviceName, kRegion);
        return HashHMACSHA256("aws4_request", kService);
    }

    static byte[] HashHMACSHA256(string data, byte[] key)
    {
        using (HMACSHA256 hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
    }

    public static string GeneratePreSignedURL(
           IAmazonS3 client,
           string bucketName,
           string objectKey,
           double duration)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddHours(duration),
        };

        string url = client.GetPreSignedURL(request);
        return url;
    }
}






