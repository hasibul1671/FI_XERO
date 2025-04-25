using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace FinanceIntegration.Model
{
    public class ConfigurationItem : TableEntity
    {
        public string RealmId { get { return PartitionKey; } }
        public string Name { get { return RowKey; } }
        public string RefreshToken { get; set; } = null;
    }

    public class AzureTableConfiguration : List<ConfigurationItem>
    {

        public AzureTableConfiguration()
        {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Configuration.StorageAccountStringConnection);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                CloudTable table = tableClient.GetTableReference("XeroAccess");
                AddRange(table.ExecuteQuery(new TableQuery<ConfigurationItem>()));
            
        }

        public void Update()
        {       
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Configuration.StorageAccountStringConnection);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                CloudTable table = tableClient.GetTableReference("XeroAccess");
                foreach (ConfigurationItem item in this)
                {
                    TableOperation update = TableOperation.Replace(item);
                    table.Execute(TableOperation.Replace(item));
                }
            
        }

    }
}
