using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class ServiceResponse
    {
        [JsonProperty("fps")]
        public int Fps { get; set; }

        [JsonProperty("cfps")]
        public int Cfps { get; set; }

        [JsonProperty("totalRequests")]
        public int TotalRequests { get; set; }

        [JsonProperty("totalCompleted")]
        public int TotalCompleted { get; set; }
    }
}
