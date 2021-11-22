using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShared
{
    public class ServiceBusConfig
    {
        public const string CONFIG_NAME = "ServiceBus";

        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
    }
}
