using System;
using System.Collections.Generic;
using System.Text;

namespace iothub_monitor
{
    public class IoTConnectedDevice
    {
        public long krcentral { get; set; }
        public long krsouth { get; set; }
        public DateTime timestamp { get; set; }
    }
}
