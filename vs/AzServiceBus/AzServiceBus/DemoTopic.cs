using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;

namespace AzServiceBus
{
    public class DemoTopic
    {
        const string TOPIC_NAME = "testtopic";

        private string _ConnectionString;
        private TopicClient _TopicClient;
        private ManagementClient _ManagementClient;
        private SubscriptionClient _SubscriptionClient;

        public DemoTopic(string connectionString)
        {
            _ConnectionString = connectionString;
        }

        public async Task Go()
        {
            _TopicClient = new TopicClient(_ConnectionString, TOPIC_NAME);
            
            _ManagementClient = new ManagementClient(_ConnectionString);

            var subscriptionId = Guid.NewGuid().ToString();
            var subscriptionDescription = new SubscriptionDescription(TOPIC_NAME, subscriptionId)
            {
                AutoDeleteOnIdle = TimeSpan.FromMinutes(30)
            };
            await _ManagementClient.CreateSubscriptionAsync(subscriptionDescription);

            _SubscriptionClient = new SubscriptionClient(_ConnectionString, TOPIC_NAME, subscriptionId);

            var messageHandlerOptions = new MessageHandlerOptions(OnExceptionReceived)
            {
                MaxConcurrentCalls = 20,
                AutoComplete = false
            };
            _SubscriptionClient.RegisterMessageHandler(OnMessageReveived, messageHandlerOptions);

            var random = new Random();
            while (true)
            {
                Console.WriteLine("Press enter to send a some messages to the topic");
                Console.ReadLine();

                var count = random.Next(1, 5);
                for (int i = 0; i < count; i++)
                {
                    var message = $"Random {random.Next(0, 1000)}";
                    Console.WriteLine($"Sending: {message}");

                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    await _TopicClient.SendAsync(new Message(messageBytes));
                }
            }
        }

        private async Task OnMessageReveived(Message message, CancellationToken token)
        {
            Console.WriteLine($"Message {message.MessageId} received: {Encoding.UTF8.GetString(message.Body)}");

            await _SubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }
        private async Task OnExceptionReceived(ExceptionReceivedEventArgs args)
        {
            Console.WriteLine($"Exception occured: {args.Exception.Message}");
        }
    }
}
