using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CrmToAnalizCardsConsumerLibrary;
using CrmToAnalizCardsProducerLibrary;
using CrmToAnalizClientInfoConsumerLibrary;
using CrmToAnalizClientInfoProducerLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncLibrary;

namespace SyncRunnerCrmToAnaliz
{
    public class Startup
    {
        private IConfigurationRoot Configuration { get; }
        private readonly IServiceProvider serviceProvider;
        private IEnumerable<IService> services;
        private List<Task> tasks = new List<Task>();

        public Startup()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            try
            {
                Configuration = builder.Build();
            }

            catch (FileNotFoundException)
            {
                Console.WriteLine("Config not found");
                Environment.Exit(1);
            }

            ConfigureServices(serviceCollection);
            serviceProvider = serviceCollection.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                services = scope.ServiceProvider.GetServices<IService>();
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddSingleton<IService, CrmToAnalizCardsConsumer>();
            services.AddSingleton<IService, CrmToAnalizCardsProducer>();
            services.AddSingleton<IService, CrmToAnalizClientInfoConsumer>();
            services.AddSingleton<IService, CrmToAnalizClientInfoProducer>();
        }

        public void LoadConfigurations()
        {
            foreach (var service in services)
            {
                service.LoadConfig(Configuration);
            }
        }

        public void Start()
        {
            // Add Quartz as a Chron engine
            foreach (var service in services)
                tasks.Add(Task.Run(() =>
                {
                    service.Start();
                }));

            Task.WhenAll(tasks);
        }
    }
}
