using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WindowsServiceCoreSample.Internal;

namespace WindowsServiceCoreSample.Logging
{
    #region public types declarations
    public class TraceSourceLoggerOptions
    {
        public bool IncludeScopes { get; set; }
    }
    #endregion

    public class TraceSourceLogger : ILogger
    {
        #region member types declarations
        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
        #endregion

        private static readonly string MessagePadding = new string(' ', 2);
        private readonly string Name;
        private readonly Func<string, LogLevel, bool> Filter;

        internal IExternalScopeProvider ScopeProvider { get; set; }

        public TraceSourceLogger(string name, Func<string, LogLevel, bool> filter, IExternalScopeProvider scopeProvider = null)
        {
            this.Name = string.IsNullOrEmpty(name) ? nameof(TraceSourceLogger) : name;
            this.Filter = filter;
            this.ScopeProvider = scopeProvider;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NoopDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            //If the filter is null, everything is enabled
            return logLevel != LogLevel.None && (this.Filter == null || this.Filter(this.Name, logLevel));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (exception is OperationCanceledException && this.Name.Equals("Microsoft.AspNetCore.Session.SessionMiddleware"))
            {
                //Ignore error: Error closing the session - OperationCanceledException: The operation was canceled.
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var sbMessage = new System.Text.StringBuilder(message);

            //Scope information
            GetScopeInformation(sbMessage);

            if (exception != null)
            {
                sbMessage.AppendLine();

                string text;
                try
                {
                    text = exception.ToString();
                }
                catch (Exception)
                {
                    text = $"{exception.GetType().ToString()}: {exception.Message}";
                }
                sbMessage.Append(text);
            }

            string str = $"{GetLogLevelName(logLevel)}[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {this.Name}{Environment.NewLine}{new string(' ', 9)}{sbMessage.ToString()}";
            System.Diagnostics.Trace.WriteLine(str);
        }

        private void GetScopeInformation(System.Text.StringBuilder stringBuilder)
        {
            var scopeProvider = this.ScopeProvider;
            if (scopeProvider != null)
            {
                int initialLength = stringBuilder.Length;

                scopeProvider.ForEachScope((scope, state) =>
                {
                    var (builder, length) = state;
                    bool first = length == builder.Length;
                    builder.Append(first ? "=> " : " => ").Append(scope);
                }, (stringBuilder, initialLength));

                if (stringBuilder.Length > initialLength)
                {
                    stringBuilder.Insert(initialLength, MessagePadding);
                    stringBuilder.AppendLine();
                }
            }
        }

        private string GetLogLevelName(LogLevel logLevel)
        {
            if (logLevel == LogLevel.Information)
            {
                return string.Format("{0,-9}", "Info:");
            }

            return string.Format("{0,-9}", logLevel.ToString() + ":");
        }
    }

    [ProviderAlias("TraceSource")]
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public class TraceSourceLoggerProvider : ILoggerProvider, ISupportExternalScope
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        private readonly ConcurrentDictionary<string, TraceSourceLogger> Loggers = new ConcurrentDictionary<string, TraceSourceLogger>();
        private readonly Func<string, LogLevel, bool> Filter;
        private readonly IDisposable OptionsReloadToken;
        private bool IncludeScopes;
        private IExternalScopeProvider ScopeProvider;

        public TraceSourceLoggerProvider(IConfiguration configuration, IOptionsMonitor<TraceSourceLoggerOptions> options)
        {
            Check.NotNull(configuration, nameof(configuration));

            this.OptionsReloadToken = options.OnChange(ReloadLoggerOptions);
            ReloadLoggerOptions(options.CurrentValue);
        }

        public TraceSourceLoggerProvider(Func<string, LogLevel, bool> filter)
        {
            this.Filter = filter;
        }

        public ILogger CreateLogger(string name)
        {
            return this.Loggers.GetOrAdd(name, CreateLoggerImplementation);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            this.ScopeProvider = scopeProvider;
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            IDisposable optionsReloadToken = this.OptionsReloadToken;
            optionsReloadToken?.Dispose();
        }

        private TraceSourceLogger CreateLoggerImplementation(string name)
        {
            return new TraceSourceLogger(name, this.Filter, this.IncludeScopes ? this.ScopeProvider : null);
        }

        private void ReloadLoggerOptions(TraceSourceLoggerOptions options)
        {
            this.IncludeScopes = options.IncludeScopes;
            var scopeProvider = GetScopeProvider();

            foreach (var logger in this.Loggers.Values)
            {
                logger.ScopeProvider = scopeProvider;
            }
        }

        private IExternalScopeProvider GetScopeProvider()
        {
            if (this.IncludeScopes && this.ScopeProvider == null)
            {
                this.ScopeProvider = new LoggerExternalScopeProvider();
            }

            return this.IncludeScopes ? this.ScopeProvider : null;
        }
    }
}