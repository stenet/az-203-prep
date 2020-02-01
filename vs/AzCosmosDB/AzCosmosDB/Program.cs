using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AzCosmosDB
{
    class Program
    {
        private const string DATABASE_ID = "testdb";
        private const string COLLECTION_ID = "people";

        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                .AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();

            var section = configuration.GetSection("CosmosDB");
            var endpoint = section["EndpointUrl"];
            var key = section["AuthorizationKey"];

            DoWork(endpoint, key).Wait();
        }

        async static Task DoWork(string endpoint, string key)
        {
            var client = new DocumentClient(new Uri(endpoint), key);

            var databaseLink = UriFactory.CreateDatabaseUri(DATABASE_ID);
            var collectionLink = UriFactory.CreateDocumentCollectionUri(DATABASE_ID, COLLECTION_ID);

            var database = await client.CreateDatabaseIfNotExistsAsync(new Database()
            {
                Id = DATABASE_ID
            });

            var collection = new DocumentCollection()
            {
                Id = COLLECTION_ID
            };

            collection.PartitionKey.Paths.Add("/Country");
            collection = await client.CreateDocumentCollectionIfNotExistsAsync(databaseLink, collection);

            var person1 = new Person("A", "A", "AT");
            await client.UpsertDocumentAsync(collectionLink, person1);

            var person2 = new Person("B", "B", "AT");
            await client.UpsertDocumentAsync(collectionLink, person2);

            var person3 = new Person("C", "C", "AT");
            await client.UpsertDocumentAsync(collectionLink, person3);

            var person4 = new Person("D", "D", "DE");
            await client.UpsertDocumentAsync(collectionLink, person4);

            var allPersonList = client
                .CreateDocumentQuery<Person>(collectionLink)
                .ToList();

            var personWithFirstNameDList = client
                .CreateDocumentQuery<Person>(collectionLink, new FeedOptions()
                {
                    EnableCrossPartitionQuery = true
                })
                .Where(c => c.FirstName == "D")
                .ToList();


            foreach (var item in personWithFirstNameDList)
            {
                item.FirstName = "DD";

                await client.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(DATABASE_ID, COLLECTION_ID, item.Id),
                    item,
                    new RequestOptions() { PartitionKey = new PartitionKey(item.Country) });
            }

            var personWithPartitionKeyATList = client
                .CreateDocumentQuery<Person>(collectionLink, new FeedOptions()
                {
                    PartitionKey = new PartitionKey("AT")
                })
                .ToList();

            foreach (var item in personWithPartitionKeyATList)
            {
                await client.DeleteDocumentAsync(
                    UriFactory.CreateDocumentUri(DATABASE_ID, COLLECTION_ID, item.Id),
                    new RequestOptions() { PartitionKey = new PartitionKey(item.Country) });
            }
        }
    }
    public class Person
    {
        public Person()
        {
        }
        public Person(string firstName, string lastName, string country)
        {
            FirstName = firstName;
            LastName = lastName;
            Country = country;
        }

        [JsonProperty("id")]
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Country { get; set; }
    }
}
