using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace iothub_monitor
{
    public class IoTHealthEntry : TableEntity
    {
        public IoTHealthEntry(string region, string center)
        {
            this.PartitionKey = region;
            this.RowKey = center;
        }

        public IoTHealthEntry() { }

        public  bool healthy { get; set; }

        public bool reprovision { get; set; }
    }
}
