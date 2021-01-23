using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WindowsServiceCoreSample.Internal;

namespace WindowsServiceCoreSample
{
    public class Worker : BackgroundService
    {
        private readonly IConfiguration Configuration;
        private readonly ILogger<Worker> Logger;

        private readonly string ServiceDisplayName;

        public Worker(IConfiguration configuration, ILogger<Worker> logger)
        {
            this.Configuration = configuration;
            this.Logger = logger;

            this.ServiceDisplayName = GetServiceDisplayName();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.Logger.LogInformation($"Service {this.ServiceDisplayName} started.");

            try
            {
                await BackgroundProcessing(stoppingToken);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(0, ex, $"Service {this.ServiceDisplayName} failed." + Environment.NewLine + ex.Message);
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);

            this.Logger.LogInformation($"Service {this.ServiceDisplayName} stopped.");
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                this.Logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                //Do Work

                await Task.Delay(1000, stoppingToken);
            }
        }

        private static string GetServiceDisplayName()
        {
            if (!WindowsServiceHelpers.IsWindowsService())
            {
                return $"{System.Diagnostics.Process.GetCurrentProcess().ProcessName} (console)";
            }

            //Make sure you do not attempt this before service Run() is called.
            int processId = Environment.ProcessId;

            return ServiceProccssInfo.GetServiceByProcessId(processId)?.DisplayName ?? $"{System.Diagnostics.Process.GetCurrentProcess().ProcessName} ({processId})";
        }
    }
}