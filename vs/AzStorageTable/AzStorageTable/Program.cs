using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace AzStorageTable
{
    class Program
    {
        const string CONNECTION_STRING = "";

        static void Main(string[] args)
        {
            var storageAccount = CloudStorageAccount.Parse(CONNECTION_STRING);
            var tableClient = storageAccount.CreateCloudTableClient();

            var table = tableClient.GetTableReference("test");
            table.CreateIfNotExistsAsync();

            var person = new PersonEntity()
            {
                PartitionKey = "AT",
                RowKey = "1",
                FirstName = "A",
                LastName = "B"
            };

            var insertOperation = TableOperation.Insert(person);
            table.ExecuteAsync(insertOperation).Wait();

            person.LastName = "X";
            var replaceOperation = TableOperation.InsertOrReplace(person);
            table.ExecuteAsync(replaceOperation).Wait();

            var condition = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "AT"),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("LastName", QueryComparisons.Equal, "X"));

            var query = new TableQuery<PersonEntity>().Where(condition);
            var result = table.ExecuteQuerySegmentedAsync(query, null).Result;

            var personResult = result.Results[0];
        }
    }
    class PersonEntity : TableEntity
    {
        public PersonEntity() { }

        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
