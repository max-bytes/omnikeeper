using Microsoft.Extensions.Logging;
using Npgsql.Logging;
using System;

namespace Omnikeeper.Base.Utils
{
    public class NpgsqlLoggingProvider : INpgsqlLoggingProvider
    {
        private readonly ILoggerFactory loggerFactory;
        //private readonly DBConnectionBuilder dBConnectionBuilder;

        public NpgsqlLoggingProvider(ILoggerFactory loggerFactory)//, DBConnectionBuilder dBConnectionBuilder)
        {
            this.loggerFactory = loggerFactory;
            //this.dBConnectionBuilder = dBConnectionBuilder;
        }

        public NpgsqlLogger CreateLogger(string name)
        {
            return new MyNpgsqlLogger(loggerFactory);//, dBConnectionBuilder);
        }
    }

    public class MyNpgsqlLogger : NpgsqlLogger
    {
        private readonly ILogger applicationLogger;
        private readonly ILogger otherLogger;
        //private readonly DBConnectionBuilder dBConnectionBuilder;

        internal MyNpgsqlLogger(ILoggerFactory loggerFactory)//, DBConnectionBuilder dBConnectionBuilder)
        {
            applicationLogger = loggerFactory.CreateLogger("npgsql.application");
            otherLogger = loggerFactory.CreateLogger("npgsql.other");
            //this.dBConnectionBuilder = dBConnectionBuilder;
        }

        public override bool IsEnabled(NpgsqlLogLevel level)
        {
            var msLevel = ToMSLogLevel(level);
            return applicationLogger.IsEnabled(msLevel) || otherLogger.IsEnabled(msLevel);
        }

        public override void Log(NpgsqlLogLevel level, int connectorId, string msg, Exception? exception = null)
        {
            if (NpgsqlConnectionWrapper.HasConnectorID(connectorId))
                applicationLogger.Log(ToMSLogLevel(level), exception, msg);
            else
                otherLogger.Log(ToMSLogLevel(level), exception, msg);
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
