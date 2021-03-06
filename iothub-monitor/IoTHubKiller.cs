using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.Devices;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace iothub_monitor
{
    public static class IoTHubKiller
    {
        [FunctionName("IoTHubKiller")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "kill")] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            bool desiredHealthy;

            if (!bool.TryParse(req.Query["healthy"], out desiredHealthy))
            {
                log.LogError("Query string is needed. [health=true]");
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var storageConnectoinString = config.GetConnectionString("StorageConnectionString");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectoinString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Get a reference to a table named "peopleTable"
            CloudTable iothealthTable = tableClient.GetTableReference("iothealth");


            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<IoTHealthEntry>(config.GetValue<string>("MonitorRegion"), config.GetValue<string>("MonitorCenter"));

            // Execute the retrieve operation.
            TableResult retrievedResult = await iothealthTable.ExecuteAsync(retrieveOperation);

            if (retrievedResult.Result != null)
            {
                var result = (IoTHealthEntry)retrievedResult.Result;
                result.healthy = desiredHealthy;

                TableOperation updateOperation = TableOperation.InsertOrReplace(result);

                TableResult updateResult = await iothealthTable.ExecuteAsync(updateOperation);

                
                return new OkObjectResult(JsonConvert.SerializeObject(result));
            }
            else
            {
                return new BadRequestObjectResult("there is no iot hub status");
            }

        }
    }
}
