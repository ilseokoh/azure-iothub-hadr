using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace KevinOh.Function
{
    public static class IoTHubHealthMonitor
    {
        [FunctionName("IoTHubHealthMonitor")]
        public static void Run([TimerTrigger("*/30 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // check iothub korea south health. 
            
        }
    }
}
