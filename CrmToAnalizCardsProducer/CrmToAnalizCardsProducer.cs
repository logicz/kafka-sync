using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CrmToAnalizCardsProducerLibrary
{
    public class CrmToAnalizCardsProducer : SyncLibrary.Producer
    {
        public override Dictionary<string, Dictionary<string, string>> Produce(SqlConnection connection, DateTime offset)
        {
            return GetData(connection, offset);
        }

        private Dictionary<string, Dictionary<string, string>> GetData(SqlConnection connection, DateTime offset)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            try
            {
              // add logic here
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
    }
}