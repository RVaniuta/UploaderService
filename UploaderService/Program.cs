using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

class Program
{
    public static List<byte[]> generatedBytes = new List<byte[]>();

    static async Task Main(string[] args)
    {
        string accessKey = "oDvlANSpdkreqwpo";
        string secretKey = "f5Zhdxyys8fO2ye8mvjBrnm3skgts3gtgaImIseX";
        string bucketName = "dev";
        int numFiles = 500;
        int minFileSize = 1024; // 1KB
        int maxFileSize = 102400; // 100KB

        AmazonS3Config cfg = new AmazonS3Config { ServiceURL = "https://s3.tebi.io" };
        AmazonS3Client s3Client = new AmazonS3Client(accessKey, secretKey, cfg);

        ConcurrentBag<Task> uploadTasks = new ConcurrentBag<Task>();

        var watch = new System.Diagnostics.Stopwatch();

        watch.Start();

        Parallel.For(1, numFiles, number => {
            string key = $"file{number}.dat";
            byte[] fileBytes = GenerateRandomFile(minFileSize, maxFileSize);
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = new MemoryStream(fileBytes)
            };
            Task uploadTask = s3Client.PutObjectAsync(request);
            uploadTasks.Add(uploadTask);
        });

        await Task.WhenAll(uploadTasks);

        watch.Stop();

        Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");

        Console.WriteLine("All files uploaded to S3.");
    }

    static byte[] GenerateRandomFile(int minSize, int maxSize)
    {
        Random random = new Random();
        int fileSize = random.Next(minSize, maxSize + 1);
        byte[] fileBytes = new byte[fileSize];
        random.NextBytes(fileBytes);
        return fileBytes;
    }
}
