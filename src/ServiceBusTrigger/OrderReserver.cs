using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using static ServiceBusTrigger.Startup;

namespace ServiceBusTrigger
{
    public class OrderReserver
    {
        private readonly StorageConfig _storageSettings;
        private readonly LogicAppConfig _logicAppConfig;

        public OrderReserver(StorageConfig storageConfig, LogicAppConfig logicAppConfig)
        {
            _storageSettings = storageConfig;
            _logicAppConfig = logicAppConfig;
        }

        [FunctionName("OrderReserver")]
        public void Run([ServiceBusTrigger("ordermessages", Connection = "OrderServiceBus")] Message message, ILogger log)
        {
            try
            {
                SaveToBlob(message.Body).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                log.LogError($"An error occured: {e.Message}");
                new HttpClient().PostAsync(
                    _logicAppConfig.Url,
                    new StringContent(Encoding.UTF8.GetString(message.Body), Encoding.UTF8, "application/json")
                ).GetAwaiter().GetResult();
            }
        }

        private async Task SaveToBlob(byte[] order)
        {
            if (order == null)
            {
                throw new Exception("Order can not be null");
            }

            var options = new BlobClientOptions();
            options.Retry.MaxRetries = 3;

            var serviceClient = new BlobServiceClient(_storageSettings.ConnectionString, options);
            var containerClient = serviceClient.GetBlobContainerClient(_storageSettings.FileContainerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString());

            using (var stream = new MemoryStream(order))
            {
                await blobClient.UploadAsync(stream);
            }
        }
    }
}
