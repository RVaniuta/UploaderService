using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        string accessKey = "oDvlANSpdkreqwpo";
        string secretKey = "f5Zhdxyys8fO2ye8mvjBrnm3skgts3gtgaImIseX";
        string bucketName = "dev";
        int numFiles = 500;
        int minFileSize = 1024; // 1KB
        int maxFileSize = 102400; // 100KB

        var svc = new ServiceCollection();

        svc.AddHttpClient("tabi", config =>
        {
            config.DefaultRequestHeaders.Add("Authorization", $"TB-PLAIN {accessKey}:{secretKey}");
        });

        var serviceProvider = svc.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

        AmazonS3Config cfg = new AmazonS3Config { ServiceURL = "https://s3.tebi.io" };
        AmazonS3Client s3Client = new AmazonS3Client(accessKey, secretKey, cfg);

        uploadTasks.Add(Monitor());

        string url = $"https://{bucketName}.s3.tebi.io/";
        //string date = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        //string amzDate = date.Substring(0, 8);
        //string signature = OneTimeHmac(accessKey, secretKey, date, "eu", bucketName, "test");

        //httpClient.DefaultRequestHeaders.Add("x-amz-date", date);
        //httpClient.DefaultRequestHeaders.Add("x-amz-content-sha256", "UNSIGNED-PAYLOAD");
        //httpClient.DefaultRequestHeaders.Add("Authorization", $"AWS4-HMAC-SHA256 Credential={accessKey}/{amzDate}/eu/s3/aws4_request, SignedHeaders=host;x-amz-date, Signature={signature}");

        //UTF8Encoding encoding = new UTF8Encoding();
        //HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(secretKey));
        //string signature = Convert.ToBase64String(hmac.ComputeHash(encoding.GetBytes(tosign)));

        //var query = Uri.EscapeDataString(signature);

        //var watch3 = new System.Diagnostics.Stopwatch();
        //watch3.Start();
        //var test = GeneratePreSignedURL(s3Client, bucketName, "test", 1000);
        //Console.WriteLine($"1 {watch3.ElapsedMilliseconds}");

        httpClient.DefaultRequestHeaders.Add("Authorization", $"TB-PLAIN {accessKey}:{secretKey}");

        //string key = $"file.dat";
        //byte[] fileBytes = GenerateRandomFile(minFileSize, maxFileSize);
        //var t = await httpClient.PutAsync($"https://dev.s3.tebi.io/test", new StreamContent(new MemoryStream(fileBytes)));

        while (true)
        {
            var watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();

            var client = httpClientFactory.CreateClient("tabi");


            Parallel.ForEach(
                Enumerable.Range(0, numFiles),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                number =>
                {
                    //Console.WriteLine($"1 {watch2.ElapsedMilliseconds}");
                    string key = $"file{number}.dat";
                    //Console.WriteLine($"2 {watch2.ElapsedMilliseconds}");
                    byte[] fileBytes = GenerateRandomFile(minFileSize, maxFileSize);
                    //Console.WriteLine($"3 {watch2.ElapsedMilliseconds}");
                    var content = new StreamContent(new MemoryStream(fileBytes));
                    //Console.WriteLine($"4 {watch2.ElapsedMilliseconds}");
                    Task uploadTask = client.PutAsync($"https://dev.s3.tebi.io/{key}", content);
                    //Task uploadTask = s3Client.PutObjectAsync(request);
                    //Task uploadTask = httpClient.PutAsync(request);
                    //var test = await uploadTask;
                    //Console.WriteLine($"5 {watch2.ElapsedMilliseconds}");
                    uploadTasks.Add(uploadTask);
                    //Console.WriteLine($"6 {watch2.ElapsedMilliseconds}");
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






