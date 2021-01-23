using System;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WindowsServiceCoreSample.Internal;
using WindowsServiceCoreSample.Logging;

namespace Microsoft.Extensions.Logging
{
    public static class TraceSourceLoggerFactoryExtensions
    {
        #region action methods
        /// <summary>
        /// Adds <see cref="TraceSourceLogger"/> logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        public static ILoggingBuilder AddTraceSource(this ILoggingBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            builder.AddConfiguration();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TraceSourceLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<TraceSourceLoggerOptions, TraceSourceLoggerProvider>(builder.Services);
            return builder;
        }

        /// <summary>
        /// Adds <see cref="TraceSourceLogger"/> logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        /// <param name="configure">TraceSource logger options</param>
        public static ILoggingBuilder AddTraceSource(this ILoggingBuilder builder, Action<TraceSourceLoggerOptions> configure)
        {
            Check.NotNull(builder, nameof(builder));
            Check.NotNull(configure, nameof(configure));

            builder.AddTraceSource();
            builder.Services.Configure(configure);
            return builder;
        }
        #endregion
    }
}