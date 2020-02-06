using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AzSearch
{
    class Program
    {
        const string SERVICE_NAME = "search2020";
        const string API_KEY = "";
        const string INDEX_NAME = "index2020";

        static void Main(string[] args)
        {
            var serviceClient = new SearchServiceClient(
                SERVICE_NAME,
                new SearchCredentials(API_KEY));

            if (serviceClient.Indexes.Exists(INDEX_NAME))
                serviceClient.Indexes.Delete(INDEX_NAME);

            serviceClient.Indexes.Create(new Microsoft.Azure.Search.Models.Index()
            {
                Name = INDEX_NAME,
                Fields = new List<Field>()
                {
                    new Field("Id", DataType.String) { IsKey = true, IsRetrievable = true },
                    new Field("Description", DataType.String) { IsSearchable = true, IsRetrievable = true }
                }
            });

            var indexClient = serviceClient.Indexes.GetClient(INDEX_NAME);

            var path = @"c:\temp";
            var pattern = "*.txt";
            var files = Directory.GetFiles(path, pattern);

            var items = new List<object>();

            foreach (var filePath in files)
            {
                var text = File.ReadAllText(filePath);
                items.Add(new
                {
                    Id = Convert.ToBase64String(Encoding.UTF8.GetBytes(filePath)),
                    Description = text
                });
            }

            var batch = IndexBatch.Upload(items);
            indexClient.Documents.Index(batch);

            var searchClient = new SearchIndexClient(
                SERVICE_NAME,
                INDEX_NAME,
                new SearchCredentials(API_KEY));

            var searchResult = searchClient.Documents.Search("peta");
            
            var resultItem = searchResult.Results.FirstOrDefault();
            if (resultItem == null)
                return;

            var resultFilePath = Encoding.UTF8.GetString(Convert.FromBase64String((string)resultItem.Document["Id"]));
            return;
        }
    }
}
