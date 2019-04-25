using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace iothub_monitor
{
    public static class IoTHubComeback
    {
        [FunctionName("IoTHubComeback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "comeback")] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(context.FunctionAppDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(config.GetConnectionString("IoTHubConnectionString"));

            var query = registryManager.CreateQuery("SELECT * FROM devices ", 100);

            var devs = new List<Device>();
            var count = 0;

            while (query.HasMoreResults)
            {
                IEnumerable<Twin> twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);

                foreach (var twin in twins)
                {
                    var dev = await registryManager.GetDeviceAsync(twin.DeviceId);
                    //var dev = await registryManager.GetDeviceAsync(twin.DeviceId);
                    //dev.Status = DeviceStatus.Disabled;
                    devs.Add(dev);
                }

                count += devs.Count;
            }

            if (count > 0) await registryManager.RemoveDevices2Async(devs);

            //--------------------

            RegistryManager registryManager_south = RegistryManager.CreateFromConnectionString(config.GetConnectionString("IoTHubConnectionString_south"));

            var devs_south = new List<Device>();
            count = 0;

            var query_south = registryManager_south.CreateQuery("SELECT * FROM devices ", 100);

            while (query_south.HasMoreResults)
            {
                IEnumerable<Twin> twins_south = await query_south.GetNextAsTwinAsync().ConfigureAwait(false);

                foreach (var twin in twins_south)
                {
                    var dev = await registryManager_south.GetDeviceAsync(twin.DeviceId);
                    //var dev = await registryManager.GetDeviceAsync(twin.DeviceId);
                    //dev.Status = DeviceStatus.Disabled;
                    devs_south.Add(dev);
                }

                count += devs_south.Count;
            }

            if (count > 0)   await registryManager_south.RemoveDevices2Async(devs_south);

            return new OkObjectResult(count);
        }
    }
}
