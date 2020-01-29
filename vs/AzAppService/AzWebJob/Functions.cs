using System;
using System.IO;
using Microsoft.Azure.WebJobs;

namespace AzWebJob
{
    public class Functions
    {
        public static void WriteFile([TimerTrigger("*/5 * * * * *", RunOnStartup = true)]TimerInfo _)
        {
            var path = Path.Combine(
                @"d:\\home", 
                "data.txt");

            File.WriteAllText(path, DateTime.Now.ToString("g"));
        }
    }
}
