using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaClient.Service
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string AutoOffsetReset { get; set; } = "Earliest";
        public bool EnableAutoCommit { get; set; } = true;
    }
}
