using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    public static List<byte[]> generatedBytes = new List<byte[]>();
    public static ConcurrentBag<Task> uploadTasks = new ConcurrentBag<Task>();
    public static Task monitor;
    public static Task listener;
    public static CancellationToken? _cancellationToken = null;
    public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    public static string bucketName = "dev2";
    public static AmazonS3Client s3Client;

    public static SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(60),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(20),
        MaxConnectionsPerServer = 500,
        EnableMultipleHttp2Connections = true
        
    };
    public static HttpClient httpClient = new HttpClient(socketsHttpHandler);

    public static int numFiles = 3000;

    public static System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

    public static List<byte[]> filesBytes = new List<byte[]>();

    public static Random random = new Random();

    public static long totalReqests = 0;
    public static long SuccessRequests = 0;
    public static long FailedRequests = 0;

    static async Task Main(string[] args)
    {
        if (args.Length > 0)
        {
            numFiles = int.Parse(args[0]);
        }

        var ips = Dns.GetHostEntry("dev2.s3.tebi.io");

        foreach (var ip in ips.AddressList)
        {
            Console.WriteLine(ip.ToString());
        }

        Console.ReadLine();

        string accessKey = "oDvlANSpdkreqwpo";
        string secretKey = "f5Zhdxyys8fO2ye8mvjBrnm3skgts3gtgaImIseX";
        int minFileSize = 1024; // 1KB
        int maxFileSize = 102400; // 100KB

        AmazonS3Config cfg = new AmazonS3Config { ServiceURL = "https://s3.tebi.io" };
        s3Client = new AmazonS3Client(accessKey, secretKey, cfg);

        monitor = Monitor();
        listener = Listener();

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"TB-PLAIN {accessKey}:{secretKey}");

        for (int i = minFileSize; i <= maxFileSize; i += minFileSize)
        {
            filesBytes.Add(new byte[i]);
        }

        watch.Start();
        while (true)
        {
            var watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();

            if (true) //_cancellationToken != null && !_cancellationToken.Value.IsCancellationRequested
            {
                Parallel.ForEach(
                Enumerable.Range(0, numFiles),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 5/*, CancellationToken = _cancellationToken.Value*/ },
                number =>
                {
                    //string key = $"file{number}_{Guid.NewGuid()}.dat";
                    //var ran = random.Next(0, 99);
                    //var content = new StreamContent(new MemoryStream(filesBytes[ran]));
                    //Task uploadTask = httpClient.PutAsync($"https://{bucketName}.s3.tebi.io/{key}", content/*, _cancellationToken.Value*/);
                    //uploadTasks.Add(uploadTask);

                    //string key = $"file{number}_{Guid.NewGuid()}.dat";
                    //var ran = random.Next(0, 99);
                    //uploadTasks.Add(File.WriteAllBytesAsync($@"C:\DEV\Files\{key}", filesBytes[ran]));

                    uploadTasks.Add(Req(httpClient, number));
                });
            }

            watch2.Stop();

            if (1000 - watch2.ElapsedMilliseconds > 0)
            {
                await Task.Delay(1000 - (int)watch2.ElapsedMilliseconds);
            }
        }
    }

    public static async Task Monitor()
    {
        var ipEndPoint = new IPEndPoint(IPAddress.Any, 1338);
        TcpListener listener = new(ipEndPoint);

        listener.Start();

        while (true)
        {
            await Task.Delay(1000);
            
            try
            {
                //TcpClient handler = await listener.AcceptTcpClientAsync();
                //await using NetworkStream stream = handler.GetStream();


                if (true)
                {
                    //var countCompleted = uploadTasks.Count(x => x.IsCompletedSuccessfully);
                    //var countAll = uploadTasks.Count();

                    var ttr = Interlocked.Read(ref totalReqests);
                    var ttsr = Interlocked.Read(ref SuccessRequests);
                    var ttfr = Interlocked.Read(ref FailedRequests);

                    long cfps = ttsr / (watch.ElapsedMilliseconds / 1000);
                    long fps = ttr / (watch.ElapsedMilliseconds / 1000);
                    long ffps = ttfr / (watch.ElapsedMilliseconds / 1000);

                    //try
                    //{
                    //    cfps = countCompleted / (watch.ElapsedMilliseconds / 1000);
                    //    fps = countAll / (watch.ElapsedMilliseconds / 1000);
                    //}
                    //catch (Exception)
                    //{
                    //}
                    

                    //var json = JsonConvert.SerializeObject(new { fps = fps, cfps = cfps, totalRequests = countAll, totalCompleted = countCompleted });
                    //byte[] msg = System.Text.Encoding.ASCII.GetBytes(json);

                    //if (handler != null && handler.Connected)
                    //{
                    //    stream.Write(msg, 0, msg.Length);
                    //}

                    Console.WriteLine($"{watch.ElapsedMilliseconds} ms! {fps} requests per second / {cfps} completed files per second / errors {ffps} / total req {ttr} / total success {ttsr} / total failed {ffps}");
                }
            }
            catch (Exception ex)
            {
            }
            
        }
    }

    public static async Task Req(HttpClient httpClient, int number)
    {
        try
        {
            string key = $"file{number}_{Guid.NewGuid()}.dat";
            var ran = random.Next(0, 99);
            var content = new StreamContent(new MemoryStream(filesBytes[ran]));
            Interlocked.Increment(ref totalReqests);
            var response = await httpClient.PutAsync($"https://{bucketName}.s3.tebi.io/{key}", content);

            if (response.IsSuccessStatusCode)
            {
                Interlocked.Increment(ref SuccessRequests);
            }
            else
            {
                Interlocked.Increment(ref FailedRequests);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Interlocked.Increment(ref FailedRequests);
        }
        
    }

    public static async Task Listener()
    {
        return;
        var ipEndPoint = new IPEndPoint(IPAddress.Any, 1337);
        TcpListener listener = new(ipEndPoint);

        listener.Start();

        _cancellationToken = cancellationTokenSource.Token;

        while (true)
        {
            try
            {
                TcpClient handler = await listener.AcceptTcpClientAsync();
                await using NetworkStream stream = handler.GetStream();

                Byte[] bytes = new Byte[256];
                string data = null;

                int i = stream.Read(bytes, 0, bytes.Length);

                if (i != 0)
                {
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    if (data.StartsWith("fps"))
                    {
                        DateTime currentTime = DateTime.Now;
                        DateTime nextMinute = currentTime.AddMinutes(1).AddSeconds(-currentTime.Second);

                        TimeSpan waitTime = nextMinute - currentTime;

                        //await s3Client.PutBucketAsync($"dev{nextMinute.ToString("yyyyMMddTHHmmss")}");

                        await Task.Delay(waitTime);

                        cancellationTokenSource.Cancel();

                        await Task.Delay(1000);

                        var newFps = int.Parse(data.Split("_")[1]);
                        numFiles = newFps;

                        uploadTasks.Clear();

                        cancellationTokenSource = new CancellationTokenSource();

                        _cancellationToken = cancellationTokenSource.Token;

                        watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                    }
                }
            }
            catch (Exception)
            {

            }
            
        }
    }

    static byte[] GenerateRandomFile(int minSize, int maxSize)
    {
        int fileSize = random.Next(minSize, maxSize + 1);
        byte[] fileBytes = new byte[fileSize];
        random.NextBytes(fileBytes);
        return fileBytes;
    }
}






