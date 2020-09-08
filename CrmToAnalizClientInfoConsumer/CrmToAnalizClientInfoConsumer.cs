using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace CrmToAnalizClientInfoConsumerLibrary
{
    public class CrmToAnalizClientInfoConsumer : SyncLibrary.Consumer
    {
        public override void Consume(Message<string, string> msg, SqlConnection connection)
        {
            ProcessData(msg.Value, connection);
        }

        private void ProcessData(string json, SqlConnection connection)
        {
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                   // add logic here
                    
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }
    }
}
