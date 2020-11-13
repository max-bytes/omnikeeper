using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Omnikeeper.Base.Utils;

namespace OKPluginActiveDirectoryXMLIngest.Tests
{
    class CountLogger<T> : ILogger<T>
    {
        private readonly IDictionary<LogLevel, int> counts = new Dictionary<LogLevel, int>();
        public IDisposable BeginScope<TState>(TState state)
        {
            return Disposable.Empty;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            counts.AddOrUpdate(logLevel, () => 1, (current) => current + 1);
        }

        public int GetCount(LogLevel l) => counts.GetOr(l, 0);
    }
}
