using System;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting
{
    internal static class HostingLoggerExtensions
    {
        private const int ApplicationStartupException = 6;

        public static void ApplicationError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError(ApplicationStartupException, "Application startup exception", exception);
        }

        public static void ApplicationError(this ILogger logger, EventId eventId, string message, Exception exception)
        {
            var reflectionTypeLoadException = exception as ReflectionTypeLoadException;
            if (reflectionTypeLoadException != null)
            {
                foreach (var ex in reflectionTypeLoadException.LoaderExceptions)
                {
                    message = message + Environment.NewLine + ex.Message;
                }
            }

            logger.LogCritical(eventId, message, exception);
        }
    }
}