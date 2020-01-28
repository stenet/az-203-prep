using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzBatch
{
    class Program
    {
        const string POOL_ID = "testpool";
        const string JOB_ID = "testjob";
        const string CONTAINER_NAME = "testcontainer";

        static BatchClient _BatchClient;
        static CloudBlobClient _BlobClient;

        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                .AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();

            _BatchClient = CreateBatchClient(configuration);
            _BlobClient = CreateBlobClient(configuration);

            DoWork().Wait();
        }

        private static async Task DoWork()
        {
            try
            {
                Console.WriteLine("Create pool");
                var pool = CreatePool();

                Console.WriteLine("Create job");
                var job = CreateJob(pool);

                Console.WriteLine("Create tasks");
                var taskList = new List<CloudTask>();
                for (int i = 0; i < 10; i++)
                {
                    var file = Path.GetTempFileName();
                    File.WriteAllText(file, $"Inputfile {i}, hello!");

                    var resourceFile = await UploadBlob(file);

                    taskList.Add(CreateTask($"task{i}", $"cmd /c type {resourceFile.FilePath}", resourceFile));
                }

                Console.WriteLine("Execute tasks");
                _BatchClient.JobOperations.AddTask(job.Id, taskList);

                var addedTasks = _BatchClient.JobOperations.ListTasks(job.Id);
                var timeout = TimeSpan.FromMinutes(30);

                Console.WriteLine("Wait for finish");
                _BatchClient.Utilities.CreateTaskStateMonitor().WaitAll(addedTasks, Microsoft.Azure.Batch.Common.TaskState.Completed, timeout);
            }
            catch (BatchException ex)
            {
                return;
            }
        }

        static BatchClient CreateBatchClient(IConfiguration configuration)
        {
            var batchConfiguration = configuration.GetSection("Batch");

            var credentials = new BatchSharedKeyCredentials(
                batchConfiguration["AccountUrl"],
                batchConfiguration["AccountName"],
                batchConfiguration["AccountKey"]);

            return BatchClient.Open(credentials);
        }
        static CloudBlobClient CreateBlobClient(IConfiguration configuration)
        {
            var storageConfiguration = configuration.GetSection("Storage");

            var storageConnectionString = $"DefaultEndpointsProtocol=https;AccountName={storageConfiguration["AccountName"]};AccountKey={storageConfiguration["AccountKey"]}";
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            return storageAccount.CreateCloudBlobClient();
        }

        static CloudPool CreatePool()
        {
            var pool = _BatchClient.PoolOperations.GetPool(POOL_ID);
            if (pool != null)
                return pool;

            pool = _BatchClient.PoolOperations.CreatePool(
                POOL_ID,
                "Standard_A1_v2",
                new VirtualMachineConfiguration(
                    imageReference: new ImageReference(
                        publisher: "MicrosoftWindowsServer",
                        offer: "WindowsServer",
                        sku: "2016-datacenter-smalldisk",
                        version: "latest"),
                    nodeAgentSkuId: "batch.node.windows amd64"),
                targetDedicatedComputeNodes: 2);

            pool.Commit();
            return pool;
        }
        static CloudJob CreateJob(CloudPool pool)
        {
            var job = _BatchClient.JobOperations.GetJob(JOB_ID);
            if (job != null)
                return job;

            job = _BatchClient.JobOperations.CreateJob();
            job.Id = JOB_ID;
            job.PoolInformation = new PoolInformation() { PoolId = pool.Id };

            job.Commit();

            return job;
        }
        static CloudTask CreateTask(string id, string commandline, ResourceFile resourceFile)
        {
            var task = new CloudTask(id, commandline);

            if (resourceFile != null)
                task.ResourceFiles = new List<ResourceFile>() { resourceFile };

            return task;
            
        }
        async static Task<ResourceFile> UploadBlob(string inputFilePath)
        {
            var blobName = Path.GetFileName(inputFilePath);
            
            var container = _BlobClient.GetContainerReference(CONTAINER_NAME);
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadFromFileAsync(inputFilePath);

            var sasPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Read
            };

            var sasBlobToken = blob.GetSharedAccessSignature(sasPolicy);

            return ResourceFile.FromUrl(string.Concat(blob.Uri, sasBlobToken), $@"c:\temp\{blobName}");
        }
    }
}
