using System;
using System.Collections.Generic;
using System.Text;

namespace iothub_monitor
{
    public class IoTHealthResponse
    {
        public bool healthy { get; set; }

        public bool reprovision { get; set; }
    }
}
