using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Client
{
    public partial class Form1 : Form
    {
        public static int SelectedIndex = 0;
        public static List<int> RequestCases = new List<int>() { 0, 100, 200, 500, 1000, 2000 };

        public static List<string> ips = new List<string> { "80.240.30.237" };
        public static Dictionary<string, Label> workers = new Dictionary<string, Label>();

        public static Task _Monitor;
        public static Task _Changer;

        public Form1()
        {
            InitializeComponent();
            //workers["127.0.0.1"] = this.status1;

            int lableInit = 600;

            foreach (var ip in ips)
            {
                Label namelabel = new Label();
                namelabel.Location = new Point(60, 600);
                namelabel.AutoSize = true;
                namelabel.Text = ip;
                this.Controls.Add(namelabel);

                Label statusLable = new Label();
                statusLable.Location = new Point(260, 600);
                statusLable.Text = "Status";
                statusLable.AutoSize = true;
                this.Controls.Add(statusLable);

                workers[ip] = statusLable;
            }

            Task.Factory.StartNew(() => Monitor());
            Task.Factory.StartNew(() => Changer());

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (SelectedIndex + 1 < RequestCases.Count)
            {
                SelectedIndex++;
                var rqc = RequestCases[SelectedIndex];
                label1.Text = rqc.ToString();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button_down_Click(object sender, EventArgs e)
        {
            if (SelectedIndex > 0)
            {
                SelectedIndex--;
                var rqc = RequestCases[SelectedIndex];
                label1.Text = rqc.ToString();
            }
        }

        public async Task Monitor()
        {
            while (true)
            {
                var totalReq = 0;
                var totalUp = 0;

                foreach (var kvp in workers)
                {
                    try
                    {
                        var tcp = new TcpClient(kvp.Key, 1338);

                        if (tcp.Connected)
                        {
                            this.Invoke((MethodInvoker)delegate { kvp.Value.Text = $"Online"; });
                            Byte[] bytes = new Byte[256];
                            string data = null;
                            int i;

                            await using NetworkStream stream = tcp.GetStream();

                            if ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                                var obj = JsonConvert.DeserializeObject<ServiceResponse>(data);
                                this.Invoke((MethodInvoker)delegate { kvp.Value.Text = $"{obj.Fps} request/sec | {obj.Cfps} complete/sec"; });
                                totalReq += obj.TotalRequests;
                                totalUp += obj.TotalCompleted;
                            }
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate { kvp.Value.Text = $"Offline"; });
                        }
                    }
                    catch (Exception)
                    {
                        this.Invoke((MethodInvoker)delegate { kvp.Value.Text = $"Offline"; });
                    }
                }

                if (totalReq + totalUp > 0)
                {
                    this.Invoke((MethodInvoker)delegate { this.totalR.Text = $"{totalReq}"; });
                    this.Invoke((MethodInvoker)delegate { this.totalU.Text = $"{totalUp}"; });
                }

                await Task.Delay(1000);
            }
        }

        public async Task Changer()
        {
            int lastIndex = 0;

            while (true)
            {
                if (lastIndex != SelectedIndex)
                {
                    await Task.Delay(2000);
                    var rqc = RequestCases[SelectedIndex];
                    lastIndex = SelectedIndex;

                    if (lastIndex == 0)
                    {
                        DateTime currentTime2 = DateTime.Now;
                        DateTime nextMinute2 = currentTime2.AddMinutes(1).AddSeconds(-currentTime2.Second);
                        this.Invoke((MethodInvoker)delegate { endAt.Text = nextMinute2.ToString("yyyy-MM-dd HH:mm:ss"); });
                    }
                    else
                    {
                        DateTime currentTime = DateTime.Now;
                        DateTime nextMinute = currentTime.AddMinutes(1).AddSeconds(-currentTime.Second);
                        this.Invoke((MethodInvoker)delegate { startAt.Text = nextMinute.ToString("yyyy-MM-dd HH:mm:ss"); });
                    }

                    foreach (var kvp in workers)
                    {
                        try
                        {
                            var tcp = new TcpClient(kvp.Key, 1337);

                            if (tcp.Connected)
                            {
                                this.Invoke((MethodInvoker)delegate { kvp.Value.Text = $"Online"; });
                                await using var stream = tcp.GetStream();
                                var dateTimeBytes = Encoding.UTF8.GetBytes($"fps_{rqc}");
                                await stream.WriteAsync(dateTimeBytes);
                            }
                            else
                            {
                                this.Invoke((MethodInvoker)delegate { kvp.Value.Text = $"Offline"; });
                            }
                        }
                        catch (Exception)
                        {
                            this.Invoke((MethodInvoker)delegate { kvp.Value.Text = $"Offline"; });
                        }
                        
                    }
                }

                await Task.Delay(1000);
            }
        }

        private void endAt_Click(object sender, EventArgs e)
        {

        }
    }
}