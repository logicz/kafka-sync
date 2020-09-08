using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Confluent.Kafka.Serialization;
using Confluent.Kafka;

namespace SyncLibrary
{
    public abstract class Consumer : IService
    {
        protected Dictionary<string, object> Config { get; set; }
        protected string Topic { get; set; }
        protected string ConnectionString { get; set; }
        protected static Logger logger;

        public void LoadConfig(IConfigurationRoot configuration)
        {
            Config = new Dictionary<string, object>();
            Config = configuration
            .GetSection("CrmToAnalizConsumer")
            .GetSection("client_settings")
            .GetChildren()
            .ToDictionary(x => x.Key,
                x => x.Key == "default.topic.config" ? x.GetChildren().ToDictionary(y => y.Key, y => (object)y.Value) : (object)x.Value
                );
            Topic = configuration["topic"].ToString();
            ConnectionString = configuration["connection.string"].ToString();

            logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetLogger(this.GetType().Name);
        }

        public void Start()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var consumer = new Consumer<string, string>(Config, new StringDeserializer(Encoding.UTF8), new StringDeserializer(Encoding.UTF8)))
                    {
                        consumer.OnMessage += (_, msg)
                            =>
                        {
                            logger.Info($"Topic: {msg.Topic} Partition: {msg.Partition} Offset: {msg.Offset}");

                            Consume(msg, connection);
                            
                            //var res = consumer.CommitAsync(msg).Result;
                        };

                        consumer.OnPartitionEOF += (_, end)
                            => logger.Info($"Reached end of topic {end.Topic} partition {end.Partition}, next message will be at offset {end.Offset}");

                        consumer.OnError += (_, error)
                            => logger.Error($"Error: {error}");

                        consumer.OnConsumeError += (_, msg)
                            => logger.Error($"Error consuming from topic/partition/offset {msg.Topic}/{msg.Partition}/{msg.Offset}: {msg.Error}");

                        consumer.OnOffsetsCommitted += (_, commit) =>
                        {
                            logger.Info($"[{string.Join(", ", commit.Offsets)}]");

                            if (commit.Error)
                            {
                                logger.Error($"Failed to commit offsets: {commit.Error}");
                            }
                            logger.Info($"Successfully committed offsets: [{string.Join(", ", commit.Offsets)}]");
                        };

                        consumer.OnPartitionsAssigned += (_, partitions) =>
                        {
                            logger.Info($"Assigned partitions: [{string.Join(", ", partitions)}], member id: {consumer.MemberId}");
                            var target = partitions.FindAll(x => x.Partition == 0);
                            Console.WriteLine(target);
                            foreach (var part in partitions)
                            {
                                Console.WriteLine(part.Partition);
                            };
                            consumer.Assign(target);
                        };

                        consumer.OnPartitionsRevoked += (_, partitions) =>
                        {
                            logger.Info($"Revoked partitions: [{string.Join(", ", partitions)}]");
                            consumer.Unassign();
                        };

                        consumer.OnStatistics += (_, json)
                            => logger.Info($"Statistics: {json}");

                        consumer.Subscribe(Topic);

                        logger.Info($"Subscribed to: [{string.Join(", ", consumer.Subscription)}]");

                        var cancelled = false;
                        Console.CancelKeyPress += (_, e) =>
                        {
                            e.Cancel = true; // prevent the process from terminating.
                            cancelled = true;
                        };

                        Console.WriteLine("Ctrl-C to exit.");
                        while (!cancelled)
                        {
                            consumer.Poll(TimeSpan.FromSeconds(100));
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

        abstract public void Consume(Message<string, string> msg, SqlConnection connection);
    }
}
