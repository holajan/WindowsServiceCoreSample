using System;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.DependencyInjection;
using WindowsServiceCoreSample.Internal;

namespace WindowsServiceCoreSample
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                //Sets the host lifetime to WindowsServiceLifetime, sets the Content Root, and enables logging to the event log with the application name as the default source name.
                //This is context aware and will only activate if it detects the process is running as a Windows Service.
                .UseWindowsService()
                .UseContentRoot(ApplicationContentRootResolver.GetApplicationContentRoot())
                .ConfigureHostConfiguration(delegate (IConfigurationBuilder config)
                {
                    config.AddEnvironmentVariables("DOTNET_");
                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureAppConfiguration((hostingContext, configurationBuilder) =>
                {
                    configurationBuilder
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

                    if (hostingContext.HostingEnvironment.IsDevelopment() && !string.IsNullOrEmpty(hostingContext.HostingEnvironment.ApplicationName))
                    {
                        var assembly = System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(hostingContext.HostingEnvironment.ApplicationName));
                        if (assembly != null)
                        {
                            configurationBuilder.AddUserSecrets(assembly, optional: true);
                        }
                    }

                    configurationBuilder.AddEnvironmentVariables();

                    if (args != null)
                    {
                        configurationBuilder.AddCommandLine(args);
                    }
                })
                .ConfigureLogging((hostingContext, loggingBuilder) =>
                {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        //Set Log Level Warning for EventLog logger (added by UseWindowsService)
                        loggingBuilder.AddFilter<EventLogLoggerProvider>((LogLevel level) => level >= LogLevel.Warning);
                    }

                    loggingBuilder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));

                    loggingBuilder.AddSimpleConsole(c =>
                    {
                        c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff zzz] ";
                    });
                    loggingBuilder.AddEventSourceLogger();
                    loggingBuilder.AddTraceSource();    //Add custom WindowsServiceCoreSample.Logging.TraceSourceLogger
                })
                .UseDefaultServiceProvider(delegate (HostBuilderContext context, ServiceProviderOptions options)
                {
                    bool validateOnBuild = options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                    options.ValidateOnBuild = validateOnBuild;
                })
                .UseStartup<Startup>()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
    }
}