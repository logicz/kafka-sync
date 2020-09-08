using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Confluent.Kafka.Serialization;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace SyncLibrary
{
    public abstract class Producer : IService
    {
        protected Dictionary<string, object> Config { get; set; }
        protected string Topic { get; set; }
        protected string ConnectionString { get; set; }
        protected static Logger logger;
        protected DateTime offset;

        public void LoadConfig(IConfigurationRoot configuration)
        {
            // Load producer config
            var config = new Dictionary<string, object>();
            config = configuration
                .GetSection("CrmToAnalizProducer")
                .GetSection("client_settings")
                .GetChildren()
                .ToDictionary(
                    x => x.Key,
                    x => x.Key == "default.topic.config" ? x.GetChildren().ToDictionary(y => y.Key, y => (object)y.Value) : (object)x.Value);

            var topic = configuration["topic"].ToString();
            var connectionString = configuration["connection.string"].ToString();

            DateTime.TryParse(configuration["offset"].ToString(), out offset);
        }

        public void Start()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var producer = new Producer<string, string>(Config, new StringSerializer(Encoding.UTF8), new StringSerializer(Encoding.UTF8)))
                    {
                        producer.OnError += (_, error)
                             => logger.Error($"{error}");

                        var cancelled = false;
                        Console.CancelKeyPress += (_, e) =>
                        {
                            e.Cancel = true; // prevent the process from terminating.
                            cancelled = true;
                        };

                        while (!cancelled)
                        {
                            try
                            {
                                Dictionary<string, Dictionary<string, string>> datum = Produce(connection, offset);

                                int count = 0;

                                foreach (var data in datum)
                                {
                                    var json = JsonConvert.SerializeObject(data.Value);
                                    producer.ProduceAsync(Topic, data.Key, json)
                                        .ContinueWith(task =>
                                        {
                                            logger.Info($"Partition: {task.Result.Partition}, Offset: {task.Result.Offset}, CardCode: {data.Value["Number"]}");
                                        });

                                    count++;

                                    if (count % 100 == 0)
                                    {
                                        producer.Flush(-1);
                                    }
                                    DateTime.TryParse(data.Value["UpdateDateTimeUtc"], out offset);
                                }

                                producer.Flush(-1);
                                logger.Info($"Отправлено записей: {count}");
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, "Producer step skipped with error");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //NLog: catch setup errors
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        abstract public Dictionary<string, Dictionary<string, string>> Produce(SqlConnection connection, DateTime offset);
    }
}
