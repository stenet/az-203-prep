using Microsoft.Azure.ServiceBus;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzServiceBus
{
    class Program
    {
        const string CONNECTION_STRING = "";
        
        static void Main(string[] args)
        {
            Console.WriteLine("Enter t for topic or q for queue mode");
            var mode = Console.ReadKey();
            Console.WriteLine();

            if (mode.Key == ConsoleKey.Q)
            {
                new DemoQueue(CONNECTION_STRING)
                    .Go()
                    .Wait();
            }
            else if (mode.Key == ConsoleKey.T)
            {
                new DemoTopic(CONNECTION_STRING)
                    .Go()
                    .Wait();
            }
        }
    }
}
