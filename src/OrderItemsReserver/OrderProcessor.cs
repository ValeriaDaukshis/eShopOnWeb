using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Client;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;

namespace OrderItemsReserver
{
    public class OrderProcessor
    {
        private readonly CosmosDbConfig _cosmosDbConfig;
        private readonly OrderDbConfig _orderDbConfig;

        private DocumentClient client;

        public OrderProcessor(CosmosDbConfig cosmosDbConfig, OrderDbConfig orderDbConfig)
        {
            _cosmosDbConfig = cosmosDbConfig;
            _orderDbConfig = orderDbConfig;
        }

        [FunctionName("OrderProcessor")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var order = JsonConvert.DeserializeObject<Order>(content);

            if (order == null)
            {
                return new BadRequestObjectResult("No order");
            }

            client = new DocumentClient(new Uri(_cosmosDbConfig.Endpoint), _cosmosDbConfig.AccountKey);

            try
            {
                await client.CreateDatabaseIfNotExistsAsync(new Database { Id = _orderDbConfig.DatabaseName });

                await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri(_orderDbConfig.DatabaseName),
                    new DocumentCollection { Id = _orderDbConfig.CollectionName }
                    );

                await CreateUserDocumentIfNotExists(_orderDbConfig.DatabaseName, _orderDbConfig.CollectionName, order);
            } 
            catch(Exception e)
            {
                log.LogError("An error occured: ", e);
                return new BadRequestResult();
            }

            return new OkResult();
        }

        private async Task CreateUserDocumentIfNotExists(string databaseName, string collectionName, Order order)
        {
            try
            {
                await client.ReadDocumentAsync(
                    UriFactory.CreateDocumentUri(databaseName, collectionName, order.BuyerId),
                    new RequestOptions { PartitionKey = new PartitionKey(order.BuyerId) }
                    );
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), order);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
