using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WindowsServiceCoreSample.Internal;

namespace WindowsServiceCoreSample.Logging
{
    #region public types declarations
    [System.Diagnostics.DebuggerDisplay("\\{ IncludeScopes = {IncludeScopes}, TimestampFormat = {TimestampFormat} \\}")]
    public sealed class TraceSourceLoggerOptions
    {
        #region member varible and default property initialization
        public bool IncludeScopes { get; set; }
        public string TimestampFormat { get; set; } = "[yyyy-MM-dd HH:mm:ss.fff zzz] ";
        #endregion
    }
    #endregion

    /// <summary>
    /// This logger logs messages to a trace listener by writing messages with System.Diagnostics.TraceSource.TraceEvent().
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reimplementace Microsoft.Extensions.Logging.TraceSourceLogger z Microsoft.Extensions.Logging.TraceSource.dll.
    /// </para>
    /// </remarks>
    public sealed class TraceSourceLogger : ILogger
    {
        #region constants
        private static readonly string MessagePadding = new string(' ', GetLogLevelString(LogLevel.Critical).Length + ": ".Length);
        private static readonly string NewLineWithMessagePadding = Environment.NewLine + MessagePadding;
        #endregion

        #region member varible and default property initialization
        private readonly string Name;
        private readonly TraceSourceLoggerProcessor QueueProcessor;

        [ThreadStatic]
        private static System.IO.StringWriter StringWriter;

        internal IExternalScopeProvider ScopeProvider { get; set; }
        internal TraceSourceLoggerOptions Options { get; set; }
        #endregion

        #region constructors and destructors
        internal TraceSourceLogger(string name, TraceSourceLoggerProcessor loggerProcessor)
        {
            Check.NotNull(name, nameof(name));
            Check.NotNull(loggerProcessor, nameof(loggerProcessor));

            this.Name = name;
            this.QueueProcessor = loggerProcessor;
        }
        #endregion

        #region action methods
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this.ScopeProvider?.Push(state) ?? NullScope.Instance;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (exception is OperationCanceledException && this.Name.Equals("Microsoft.AspNetCore.Session.SessionMiddleware", StringComparison.Ordinal))
            {
                //Ignore error: Error closing the session - OperationCanceledException: The operation was canceled.
                return;
            }

            Check.NotNull(formatter, nameof(formatter));

            string message = FormatLogMessage(logLevel, eventId, state, exception, formatter);

            if (message != null)
            {
                this.QueueProcessor.EnqueueMessage(message);
            }
        }
        #endregion

        #region private member functions
        private string FormatLogMessage<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (StringWriter == null)
            {
                StringWriter = new System.IO.StringWriter();
            }

            WriteLogMessage(StringWriter, logLevel, eventId, state, exception, formatter);

            var stringBuilder = StringWriter.GetStringBuilder();
            if (stringBuilder.Length == 0)
            {
                return null;
            }

            string message = stringBuilder.ToString();

            stringBuilder.Clear();
            if (stringBuilder.Capacity > 1024)
            {
                stringBuilder.Capacity = 1024;
            }

            return message;
        }

        private void WriteLogMessage<TState>(System.IO.TextWriter textWriter, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Check.NotNull(textWriter, nameof(textWriter));
            Check.NotNull(formatter, nameof(formatter));

            string message = formatter(state, exception);

            if (message == null && exception == null)
            {
                return;
            }

            string logLevelString = GetLogLevelString(logLevel);
            if (logLevelString != null)
            {
                textWriter.Write((logLevelString + ": ").PadRight(MessagePadding.Length));
            }

            string timestampFormat = this.Options.TimestampFormat;
            if (timestampFormat != null)
            {
                textWriter.Write(DateTimeOffset.Now.ToString(timestampFormat, System.Globalization.CultureInfo.CurrentCulture));
            }

            textWriter.Write(this.Name + "[" + eventId + "]");
            textWriter.Write(Environment.NewLine);
            WriteScopeInformation(textWriter);
            WriteMessage(textWriter, message);

            if (exception != null)
            {
                string text;
                try
                {
                    text = ExceptionHelper.FormatException(exception);
                }
                catch
                {
                    text = $"{exception.GetType().ToString()}: {exception.Message}";
                }

                if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(text))
                {
                    textWriter.Write(Environment.NewLine);
                }

                WriteMessage(textWriter, text);
            }
        }

        private void WriteScopeInformation(System.IO.TextWriter textWriter)
        {
            if (!this.Options.IncludeScopes || this.ScopeProvider == null)
            {
                return;
            }

            bool paddingNeeded = true;
            this.ScopeProvider.ForEachScope((scope, textWriter) =>
            {
                if (paddingNeeded)
                {
                    paddingNeeded = false;
                    textWriter.Write(MessagePadding + "=> ");
                }
                else
                {
                    textWriter.Write(" => ");
                }

                textWriter.Write(scope);
            }, textWriter);

            if (!paddingNeeded)
            {
                textWriter.Write(Environment.NewLine);
            }
        }

        private void WriteMessage(System.IO.TextWriter textWriter, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                textWriter.Write(MessagePadding);
                textWriter.Write(message.Replace(Environment.NewLine, NewLineWithMessagePadding, StringComparison.Ordinal));
            }
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            if (logLevel == LogLevel.Information)
            {
                return "Info";
            }

            return logLevel.ToString();
        }
        #endregion
    }

    [ProviderAlias("TraceSource")]
    public sealed class TraceSourceLoggerProvider : ILoggerProvider, IDisposable, ISupportExternalScope
    {
        #region member varible and default property initialization
        private readonly IOptionsMonitor<TraceSourceLoggerOptions> Options;
        private readonly ConcurrentDictionary<string, TraceSourceLogger> Loggers = new ConcurrentDictionary<string, TraceSourceLogger>();
        private readonly IDisposable OptionsReloadToken;
        private readonly TraceSourceLoggerProcessor MessageQueue;
        private IExternalScopeProvider ScopeProvider = NullExternalScopeProvider.Instance;
        #endregion

        #region constructors and destructors
        public TraceSourceLoggerProvider(IOptionsMonitor<TraceSourceLoggerOptions> options)
        {
            Check.NotNull(options, nameof(options));

            this.Options = options;

            ReloadLoggerOptions(options.CurrentValue);
            this.OptionsReloadToken = options.OnChange(ReloadLoggerOptions);

            this.MessageQueue = new TraceSourceLoggerProcessor();
        }
        #endregion

        #region action methods
        public ILogger CreateLogger(string name)
        {
            return this.Loggers.GetOrAdd(name, loggerName => new TraceSourceLogger(name, this.MessageQueue)
            {
                Options = this.Options.CurrentValue,
                ScopeProvider = this.ScopeProvider
            });
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            this.ScopeProvider = scopeProvider;

            foreach (var logger in this.Loggers)
            {
                logger.Value.ScopeProvider = this.ScopeProvider;
            }
        }

        public void Dispose()
        {
            this.OptionsReloadToken?.Dispose();
            this.MessageQueue.Dispose();
        }
        #endregion

        #region private member functions
        private void ReloadLoggerOptions(TraceSourceLoggerOptions options)
        {
            Check.NotNull(options, nameof(options));

            foreach (var logger in this.Loggers)
            {
                logger.Value.Options = options;
            }
        }
        #endregion
    }
}