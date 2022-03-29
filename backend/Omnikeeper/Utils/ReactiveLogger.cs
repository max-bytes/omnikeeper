using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Omnikeeper.Utils
{
    public class ReactiveLogger : ILogger
    {
        private readonly string categoryName;
        private readonly ReactiveLogReceiver logReceiver;

        public ReactiveLogger(string categoryName, ReactiveLogReceiver logReceiver)
        {
            this.categoryName = categoryName;
            this.logReceiver = logReceiver;
        }

        public IDisposable BeginScope<TState>(TState state) => System.Reactive.Disposables.Disposable.Empty;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;


        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            // NOTE: exception seems to be ignored and not put into message: https://github.com/aspnet/Logging/issues/442
            var message = formatter(state, exception);

            if (exception != null)
                message = message + "\n" + exception.ToString();

            logReceiver.WriteLine(new LogLine() { Category = categoryName, LogLevel = logLevel, Message = message });
        }
    }

    public class ReactiveLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();
        private readonly ReactiveLogReceiver lr;

        public ReactiveLoggerProvider(ReactiveLogReceiver lr)
        {
            this.lr = lr;
        }

        public void Dispose() => _loggers.Clear();

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new ReactiveLogger(name, lr));
    }
}
