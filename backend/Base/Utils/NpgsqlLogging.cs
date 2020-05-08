using Microsoft.Extensions.Logging;
using Npgsql.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Landscape.Base.Utils
{
    public class NpgsqlLoggingProvider : INpgsqlLoggingProvider
    {
        public NpgsqlLoggingProvider(ILogger<MyNpgsqlLogger> logger)
        {
            Logger = logger;
        }

        public ILogger<MyNpgsqlLogger> Logger { get; }

        public NpgsqlLogger CreateLogger(string name)
        {
            return new MyNpgsqlLogger(name, Logger);
        }
    }

    public class MyNpgsqlLogger : NpgsqlLogger
    {
        readonly ILogger<MyNpgsqlLogger> logger;

        internal MyNpgsqlLogger(string name, ILogger<MyNpgsqlLogger> logger)
        {
            this.logger = logger;
        }

        public override bool IsEnabled(NpgsqlLogLevel level)
        {
            return logger.IsEnabled(ToMSLogLevel(level));
        }

        public override void Log(NpgsqlLogLevel level, int connectorId, string msg, Exception exception = null)
        {
            logger.Log(ToMSLogLevel(level), exception, msg);
        }

        static LogLevel ToMSLogLevel(NpgsqlLogLevel level)
        {
            return level switch
            {
                NpgsqlLogLevel.Trace => LogLevel.Trace,
                NpgsqlLogLevel.Debug => LogLevel.Debug,
                NpgsqlLogLevel.Info => LogLevel.Information,
                NpgsqlLogLevel.Warn => LogLevel.Warning,
                NpgsqlLogLevel.Error => LogLevel.Error,
                NpgsqlLogLevel.Fatal => LogLevel.Critical,
                _ => throw new ArgumentOutOfRangeException("level"),
            };
        }
    }
}
