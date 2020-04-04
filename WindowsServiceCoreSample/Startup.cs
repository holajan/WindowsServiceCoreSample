using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WindowsServiceCoreSample
{
    public class Startup
    {
        private readonly IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var loggerFactory = GetLoggerFactory(services);

            try
            {
                //Use this to add services to the container.

            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("ConfigureServices");
                logger.ApplicationError(ex);

                throw;
            }
        }

        public void Configure(IServiceProvider serviceProvider)
        {
            //Use this to configure application
        }

        private ILoggerFactory GetLoggerFactory(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetRequiredService<ILoggerFactory>();
        }
    }
}