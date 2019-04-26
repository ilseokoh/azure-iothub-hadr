using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace iothub_monitor
{
    public static class IoTHubConnectionMonitor
    {
        [FunctionName("IoTHubConnectionMonitor")]
        public static async void Run([TimerTrigger("*/2 * * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            
            var config = new ConfigurationBuilder()
               .SetBasePath(context.FunctionAppDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(config.GetConnectionString("IoTHubConnectionString"));

            RegistryStatistics stats = await registryManager.GetRegistryStatisticsAsync();
            long krcentralTotal = stats.TotalDeviceCount;

            string queryString = "SELECT COUNT() AS numberOfConnectedDevices FROM devices WHERE connectionState = 'Connected'";
            IQuery query = registryManager.CreateQuery(queryString, 1);
            string json = (await query.GetNextAsJsonAsync()).FirstOrDefault();
            long centralCount = 0;
            if (json != null)
            {
                Dictionary<string, long> data = JsonConvert.DeserializeObject<Dictionary<string, long>>(json);
                centralCount = data["numberOfConnectedDevices"];
            }
             

            //-----------------

            RegistryManager registryManager_south = RegistryManager.CreateFromConnectionString(config.GetConnectionString("IoTHubConnectionString_south"));

            RegistryStatistics stats_south = await registryManager_south.GetRegistryStatisticsAsync();
            long krsouthTotal = stats_south.TotalDeviceCount;

            IQuery query_south = registryManager_south.CreateQuery(queryString, 1);
            string json_south = (await query_south.GetNextAsJsonAsync()).FirstOrDefault();
            long southCount = 0;
            if (json_south != null)
            {
                Dictionary<string, long> data_south = JsonConvert.DeserializeObject<Dictionary<string, long>>(json_south);
                southCount = data_south["numberOfConnectedDevices"];
            }

            //------------------

            var result = new IoTConnectedDevice
            {
                krcentral = centralCount,
                krsouth = southCount,
                timestamp = DateTime.Now.ToUniversalTime(),
                krsouthTotal = krsouthTotal,
                krcentralTotal = krcentralTotal
            };

            var jsonResult = JsonConvert.SerializeObject(result);
            var powerbiUri = "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/1e275ffc-71bc-4bf6-9531-8de3391d4027/rows?key=uejGC33nWLEgVoWmAHXoOlXwy0mhxnImmHT7VvMsg5Qtj%2FvBkoEHnJY1B%2FuIEgSQkRb9IrE1haIp19t%2FJp7Xvw%3D%3D";

            var client = new HttpClient();
            var response = await client.PostAsync(powerbiUri, new StringContent(jsonResult, Encoding.UTF8, "application/json"));

            log.LogInformation($"Send data to powerbi: {jsonResult}");
        }
    }
}
