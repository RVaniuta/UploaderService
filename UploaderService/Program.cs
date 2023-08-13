﻿using System;
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
        MaxConnectionsPerServer = 10000,
        EnableMultipleHttp2Connections = true
        
    };
    public static HttpClient httpClient = new HttpClient(socketsHttpHandler);

    public static int numFiles = 0;

    public static System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

    static async Task Main(string[] args)
    {
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

        while (true)
        {
            var watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();

            if (_cancellationToken != null && !_cancellationToken.Value.IsCancellationRequested)
            {
                Parallel.ForEach(
                Enumerable.Range(0, numFiles / 10),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _cancellationToken.Value },
                number =>
                {
                    string key = $"file{number}_{Guid.NewGuid()}.dat";
                    byte[] fileBytes = GenerateRandomFile(minFileSize, maxFileSize);
                    var content = new StreamContent(new MemoryStream(fileBytes));
                    Task uploadTask = httpClient.PutAsync($"https://{bucketName}.s3.tebi.io/{key}", content, _cancellationToken.Value);
                    uploadTasks.Add(uploadTask);
                });
            }

            watch2.Stop();

            if (100 - watch2.ElapsedMilliseconds > 0)
            {
                await Task.Delay(100 - (int)watch2.ElapsedMilliseconds);
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
                TcpClient handler = await listener.AcceptTcpClientAsync();
                await using NetworkStream stream = handler.GetStream();


                if (true)
                {
                    var countCompleted = uploadTasks.Count(x => x.IsCompletedSuccessfully);
                    var countAll = uploadTasks.Count();

                    long cfps = 0;
                    long fps = 0;

                    try
                    {
                        cfps = countCompleted / (watch.ElapsedMilliseconds / 1000);
                        fps = countAll / (watch.ElapsedMilliseconds / 1000);
                    }
                    catch (Exception)
                    {
                    }
                    

                    var json = JsonConvert.SerializeObject(new { fps = fps, cfps = cfps, totalRequests = countAll, totalCompleted = countCompleted });
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(json);

                    if (handler != null && handler.Connected)
                    {
                        stream.Write(msg, 0, msg.Length);
                    }

                    Console.WriteLine($"{watch.ElapsedMilliseconds} ms! {fps} requests per second / {cfps} completed files per second");
                }
            }
            catch (Exception ex)
            {
            }
            
        }
    }

    public static async Task Listener()
    {
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
        Random random = new Random();
        int fileSize = random.Next(minSize, maxSize + 1);
        byte[] fileBytes = new byte[fileSize];
        random.NextBytes(fileBytes);
        return fileBytes;
    }
}






