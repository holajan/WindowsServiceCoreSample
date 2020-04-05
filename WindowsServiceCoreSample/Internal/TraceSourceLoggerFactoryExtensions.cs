using System;
using Microsoft.Extensions.DependencyInjection;
using WindowsServiceCoreSample.Internal;
using WindowsServiceCoreSample.Logging;

namespace Microsoft.Extensions.Logging
{
    public static class TraceSourceLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds <see cref="TraceSourceLogger"/> logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        public static ILoggingBuilder AddTraceSource(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, TraceSourceLoggerProvider>();
            return builder;
        }

        /// <summary>
        /// Adds <see cref="TraceSourceLogger"/> logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        /// <param name="configure">Database logger options</param>
        public static ILoggingBuilder AddTraceSource(this ILoggingBuilder builder, Action<TraceSourceLoggerOptions> configure)
        {
            Check.NotNull(configure, nameof(configure));

            builder.AddTraceSource();
            builder.Services.Configure(configure);
            return builder;
        }

        /// <summary>
        /// Adds <see cref="TraceSourceLogger"/> logger that is enabled for <see cref="T:Microsoft.Extensions.Logging.LogLevel" />.Information or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory)
        {
            Check.NotNull(factory, nameof(factory));

            return factory.AddTraceSource(LogLevel.Information);
        }

        /// <summary>
        /// Adds <see cref="TraceSourceLogger"/> logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="filter">The function used to filter events based on the log level.</param>
        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, Func<string, LogLevel, bool> filter)
        {
            Check.NotNull(factory, nameof(factory));

            factory.AddProvider(new TraceSourceLoggerProvider(filter));
            return factory;
        }

        /// <summary>
        /// Adds <see cref="TraceSourceLogger"/> logger that is enabled for <see cref="T:Microsoft.Extensions.Logging.LogLevel" />s of minLevel or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="minLevel">The minimum <see cref="T:Microsoft.Extensions.Logging.LogLevel" /> to be logged</param>
        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, LogLevel minLevel)
        {
            Check.NotNull(factory, nameof(factory));

            return factory.AddTraceSource((_, logLevel) => logLevel >= minLevel);
        }
    }
}