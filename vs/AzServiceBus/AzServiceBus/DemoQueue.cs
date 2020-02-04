using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzServiceBus
{
    public class DemoQueue
    {
        private const string QUEUE_NAME = "testqueue";

        private string _ConnectionString;
        private QueueClient _QueueClient;

        public DemoQueue(string connectionString)
        {
            _ConnectionString = connectionString;
        }

        public async Task Go()
        {
            _QueueClient = new QueueClient(
                _ConnectionString,
                QUEUE_NAME);

            var messageHandlerOptions = new MessageHandlerOptions(OnExceptionReceived)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };
            _QueueClient.RegisterMessageHandler(OnMessageReveived, messageHandlerOptions);

            var random = new Random();
            while (true)
            {
                Console.WriteLine("Press enter to send a some messages to the queue");
                Console.ReadLine();

                var count = random.Next(1, 5);
                for (int i = 0; i < count; i++)
                {
                    var message = $"Random {random.Next(0, 1000)}";
                    Console.WriteLine($"Sending: {message}");

                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    await _QueueClient.SendAsync(new Message(messageBytes));
                }
            }
        }

        private async Task OnMessageReveived(Message message, CancellationToken token)
        {
            Console.WriteLine($"Message {message.MessageId} received: {Encoding.UTF8.GetString(message.Body)}");

            await _QueueClient.CompleteAsync(message.SystemProperties.LockToken);
        }
        private async Task OnExceptionReceived(ExceptionReceivedEventArgs args)
        {
            Console.WriteLine($"Exception occured: {args.Exception.Message}");
        }
    }
}
