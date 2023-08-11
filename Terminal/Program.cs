// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Net;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

var json = JsonConvert.SerializeObject(new { fps = 0, cfps = 0, totalRequests = 0, totalCompleted = 0 });

Console.ReadLine();

using var client = new TcpClient("127.0.0.1", 1337);
using var client2 = new TcpClient("127.0.0.1", 1338);
//using var client = new TcpClient("80.240.30.237", 1337);

await using var stream = client.GetStream();
var dateTimeBytes = Encoding.UTF8.GetBytes("fps_100");
await stream.WriteAsync(dateTimeBytes);

Byte[] bytes = new Byte[256];
string data = null;
int i;

await using var stream2 = client2.GetStream();

while (true)
{
    if ((i = stream2.Read(bytes, 0, bytes.Length)) != 0)
    {
        // Translate data bytes to a ASCII string.
        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
        Console.WriteLine("Received: {0}", data);
    }

    await Task.Delay(500);
}

Console.ReadLine();