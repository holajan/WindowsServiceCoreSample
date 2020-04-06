using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting
{
    internal static class HostBuilderUseStartupExtensions
    {
        #region member types declarations
        //This exists just so that we can use ActivatorUtilities.CreateInstance on the Startup class
        private class HostServiceProvider : IServiceProvider
        {
            #region member varible and default property initialization
            private readonly HostBuilderContext HostContext;
            #endregion

            #region constructors and destructors
            public HostServiceProvider(HostBuilderContext hostContext)
            {
                this.HostContext = hostContext;
            }
            #endregion

            #region action methods
            public object GetService(Type serviceType)
            {
                // The implementation of the HostingEnvironment supports both interfaces
                if (serviceType == typeof(IHostEnvironment))
                {
                    return this.HostContext.HostingEnvironment;
                }

                if (serviceType == typeof(IConfiguration))
                {
                    return this.HostContext.Configuration;
                }

                return null;
            }
            #endregion
        }

        private class StartupConfigureOptions
        {
            #region member varible and default property initialization
            public Action<IServiceProvider> Configure { get; set; }
            #endregion
        }

        private class StartupConfigureHostedService : IHostedService
        {
            #region member varible and default property initialization
            private readonly IServiceProvider ServiceProvider;
            private readonly ILogger Logger;
            private readonly StartupConfigureOptions Options;
            #endregion

            #region constructors and destructors
            public StartupConfigureHostedService(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IOptions<StartupConfigureOptions> options)
            {
                this.ServiceProvider = serviceProvider;
                this.Logger = loggerFactory.CreateLogger("Startup Configure");
                this.Options = options.Value;
            }
            #endregion

            #region action methods
            public Task StartAsync(CancellationToken cancellationToken)
            {
                try
                {
                    Action<IServiceProvider> configure = this.Options.Configure;
                    configure(this.ServiceProvider);
                }
                catch (Exception ex)
                {
                    Logger.ApplicationError(ex);
                    throw;
                }

                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
            #endregion
        }
        #endregion

        public static IHostBuilder UseStartup<TStartup>(this IHostBuilder builder)
        {
            return builder.ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<StartupConfigureHostedService>();

                var serviceProvider = services.BuildServiceProvider();
                var hostingEnvironment = serviceProvider.GetService<IHostEnvironment>();

                //Create Startup class instance
                Type startupType = typeof(TStartup);
                object startup = ActivatorUtilities.CreateInstance(new HostServiceProvider(hostContext), startupType);  //Startup(hostContext.Configuration, hostingEnvironment)

                //Run Startup.ConfigureServices(services)
                var configureServicesMethodInfo = startupType.GetMethod("ConfigureServices", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null,
                        new Type[1] { typeof(IServiceCollection) }, Array.Empty<ParameterModifier>());

                if (configureServicesMethodInfo == null)
                {
                    throw new InvalidOperationException($"Public method ConfigureServices was not found in class {startupType}.");
                }

                configureServicesMethodInfo.Invoke(startup, BindingFlags.DoNotWrapExceptions, binder: null, new object[] { services }, culture: null);

                //Startup.Configure
                services.Configure<StartupConfigureOptions>(options =>
                {
                    options.Configure = (serviceProvider) =>  //Called by StartupConfigureHostedService after IHost.Run()
                    {
                        //Run Startup.Configure(serviceProvider)
                        var configureMethodInfo = startupType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null,
                            new Type[1] { typeof(IServiceProvider) }, Array.Empty<ParameterModifier>());

                        if (configureMethodInfo == null)
                        {
                            throw new InvalidOperationException($"Public method Configure was not found in class {startupType}.");
                        }

                        configureMethodInfo.Invoke(startup, BindingFlags.DoNotWrapExceptions, binder: null, new object[] { serviceProvider }, culture: null);
                    };
                });
            });
        }
    }
}