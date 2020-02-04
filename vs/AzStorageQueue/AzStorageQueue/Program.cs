using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using System;
using System.Threading.Tasks;

namespace AzStorageQueue
{
    class Program
    {
        const string CONNECTION_STRING = "";

        static void Main(string[] args)
        {
            var storageAccount = CloudStorageAccount.Parse(CONNECTION_STRING);
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("testqueue");

            ReceiveMessageAsync(queue);

            var random = new Random();
            while (true)
            {
                Console.WriteLine("Press enter to send a message");
                Console.ReadLine();

                SendMessageAsync(queue, $"Random {random.Next(0, 1000)}").Wait();
            }
        }

        static async void ReceiveMessageAsync(CloudQueue queue)
        {
            while (true)
            {
                var message = await queue.GetMessageAsync();

                if (message != null)
                {
                    Console.WriteLine($"received: {message.AsString}");
                    await queue.DeleteMessageAsync(message);
                }
            }
        }
        static async Task SendMessageAsync(CloudQueue queue, string message)
        {
            await queue.AddMessageAsync(new CloudQueueMessage(message));
        }
    }
}
