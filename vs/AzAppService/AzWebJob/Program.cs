using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzWebJob
{
    class Program
    {
        private static IConfiguration Configuration { get; set; }

        static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "development";

            var builder = new HostBuilder()
                .UseEnvironment(environment)
                .ConfigureWebJobs(configure =>
                {
                    configure.AddAzureStorageCoreServices();
                    configure.AddTimers();
                })
                .ConfigureAppConfiguration((context, configure) =>
                {
                    configure
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{environment}.json", true, true);
                })
                .ConfigureServices(c => c.AddTransient<Functions>())
                .ConfigureLogging((context, configure) =>
                {
                    configure
                        .SetMinimumLevel(LogLevel.Debug)
                        .AddConsole();
                })
                .UseConsoleLifetime();

            var host = builder.Build();
            host.Run();
        }
    }
}
