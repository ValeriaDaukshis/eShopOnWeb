using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

[assembly: FunctionsStartup(typeof(ServiceBusTrigger.Startup))]
namespace ServiceBusTrigger
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

            var storageConfig = new StorageConfig();
            config.Bind(nameof(StorageConfig), storageConfig);

            var logicAppConfig = new LogicAppConfig();
            config.Bind(nameof(LogicAppConfig), logicAppConfig);

            builder.Services.AddSingleton(storageConfig);
            builder.Services.AddSingleton(logicAppConfig);
        }

        public class StorageConfig
        {
            public string ConnectionString { get; set; }
            public string FileContainerName { get; set; }
        }

        public class LogicAppConfig
        {
            public string Url { get; set; }
        }
    }
}
