using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Startup
{
    // NOTE: taken from https://github.com/maxstue/Serilog.Sinks.AspNetCore.SignalR, then modified
    public class MySignalRSink<THub, T> : ILogEventSink where THub : Hub<T> where T : class, IHub
    {
        private readonly IFormatProvider? _formatProvider;
        private readonly IServiceProvider _serviceProvider;
        private IHubContext<THub, T>? _hubContext;
        private readonly string[] _groups;
        private readonly string[] _userIds;
        private readonly string[] _excludedConnectionIds;
        private readonly bool _sendAsString;

        /// <summary>
        /// Sink constructor.
        /// </summary>
        /// <param name="formatProvider">The format provider with which the events are formatted.</param>
        /// <param name="sendAsString">A bool to decide as what the log should be send.</param>
        /// <param name="serviceProvider">The current serviceProvider.</param>
        /// <param name="groups">The groups where the events are sent.</param>
        /// <param name="userIds">The users to where the events are sent.</param>
        /// <param name="excludedConnectionIds">The client ids to exclude.</param>
        public MySignalRSink(IFormatProvider? formatProvider,
            bool sendAsString,
            IServiceProvider serviceProvider,
            string[]? groups = null,
            string[]? userIds = null,
            string[]? excludedConnectionIds = null)
        {
            _formatProvider = formatProvider;
            _sendAsString = sendAsString;
            _serviceProvider = serviceProvider;
            _groups = groups ?? new string[] { };
            _userIds = userIds ?? new string[] { };
            _excludedConnectionIds = excludedConnectionIds ?? new string[] { };

        }

        /// <summary>
        /// Emit a log event to the registered clients
        /// </summary>
        /// <param name="logEvent">The event to emit</param>
        public void Emit(LogEvent logEvent)
        {

            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }
            if (_hubContext == null)
            {
                _hubContext = _serviceProvider.GetRequiredService<IHubContext<THub, T>>();
            }
            var targets = new List<T>();

            if (_groups.Any())
            {
                targets.Add(_hubContext
                    .Clients
                    .Groups(_groups
                        .Except(_excludedConnectionIds)
                        .ToArray()
                    )
                );
            }

            if (_userIds.Any())
            {
                targets.Add(_hubContext
                    .Clients
                    .Users(_userIds
                        .Except(_excludedConnectionIds)
                        .ToArray()
                    )
                );
            }

            if (!_groups.Any() && !_userIds.Any())
            {
                targets.Add(_hubContext
                    .Clients
                    .AllExcept(_excludedConnectionIds)
                );
            }

            foreach (var target in targets)
            {
                var sourceContext = "";
                if (logEvent.Properties.TryGetValue("SourceContext", out var sc) && sc is ScalarValue svsc && svsc.Value is string rawValue)
                    sourceContext = rawValue;

                if (_sendAsString)
                {
                    target.SendLogAsString($"{logEvent.Timestamp:dd.MM.yyyy HH:mm:ss.fff} " +
                                           $"{logEvent.Level} " +
                                           $"{sourceContext} " +
                                           $"{logEvent.RenderMessage(_formatProvider)} " +
                                           $"{logEvent.Exception?.ToString() ?? "-"} ");
                }
                else
                {
                    var id = Guid.NewGuid().ToString();
                    var timestamp = logEvent.Timestamp.ToString("dd.MM.yyyy HH:mm:ss.fff");
                    var level = logEvent.Level.ToString();
                    var message = logEvent.RenderMessage(_formatProvider);
                    var exception = logEvent.Exception?.ToString() ?? "-";
                    target.SendLogAsObject(new { id, timestamp, level, message, exception, sourceContext });
                }
            }
        }
    }

    public interface IHub
    {
        /// <summary>
        /// Send a message as a string
        /// </summary>
        /// <param name="message">The message you want to send.</param>
        /// <returns>Task.</returns>
        Task SendLogAsString(string message);

        /// <summary>
        /// Send a message as an object
        /// </summary>
        /// <param name="messageObject">The message you want to send.</param>
        /// <returns>Task.</returns>
        Task SendLogAsObject(object messageObject);
    }

    public interface ILogHub : IHub
    {
    }

    public class SignalRHubLogging : Hub<ILogHub>
    {
    }
}
