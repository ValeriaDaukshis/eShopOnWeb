using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

[assembly: FunctionsStartup(typeof(OrderItemsReserver.Startup))]
namespace OrderItemsReserver
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var cosmosDbConfig = new CosmosDbConfig();
            config.Bind(nameof(CosmosDbConfig), cosmosDbConfig);

            var orderDbConfig = new OrderDbConfig();
            config.Bind(nameof(OrderDbConfig), orderDbConfig);

            builder.Services.AddSingleton(cosmosDbConfig);
            builder.Services.AddSingleton(orderDbConfig);

            //builder.Services.AddOptions<StorageConfig>()
            //    .Configure<IConfiguration>((settings, configuration) =>
            //    {
            //        configuration.GetSection("StorageConfig").Bind(settings);
            //    });

            //builder.Services.AddOptions<LogicAppConfig>()
            //    .Configure<IConfiguration>((settings, configuration) =>
            //    {
            //        configuration.GetSection("LogicAppConfig").Bind(settings);
            //    });

            //builder.Services.AddOptions<CosmosDbConfig>()
            //    .Configure<IConfiguration>((settings, configuration) =>
            //    {
            //        configuration.GetSection("CosmosDbConfig").Bind(settings);
            //    });

            //builder.Services.AddOptions<OrderDbConfig>()
            //    .Configure<IConfiguration>((settings, configuration) =>
            //    {
            //        configuration.GetSection("OrderDbConfig").Bind(settings);
            //    });
        }
    }

    //public class StorageConfig
    //{
    //    public string ConnectionString { get; set; }
    //    public string FileContainerName { get; set; }
    //}

    //public class LogicAppConfig
    //{
    //    public string Url { get; set; }
    //}

    public class CosmosDbConfig
    {
        public string Endpoint { get; set; }
        public string AccountKey { get; set; }
    }

    public class OrderDbConfig
    {
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
    }
}
