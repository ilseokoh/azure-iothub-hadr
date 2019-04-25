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
using Microsoft.Azure.Devices.Shared;
using System.Collections.Generic;

namespace iothub_monitor
{
    public static class IoTDevicesRemover
    {
        [FunctionName("IoTDevicesRemover")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "remove")] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(context.FunctionAppDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(config.GetConnectionString("IoTHubConnectionString"));

            var query = registryManager.CreateQuery("SELECT * FROM devices WHERE status = 'enabled'", 100);

            //var dev = new Device("123") { Status = DeviceStatus.Disabled };

            var devs = new List<Device>();
            var count = 0;

            while (query.HasMoreResults)
            {
                IEnumerable<Twin> twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);

                foreach (var twin in twins)
                {
                    //var dev = new Device(twin.DeviceId) { Status = DeviceStatus.Disabled, ETag = twin.ETag };
                    var dev = await registryManager.GetDeviceAsync(twin.DeviceId);
                    dev.Status = DeviceStatus.Disabled;
                    devs.Add(dev);
                }

                count += devs.Count;
            }

            await registryManager.UpdateDevices2Async(devs).ConfigureAwait(false);
            return new OkObjectResult(count);
        }
    }
}
