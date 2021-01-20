using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Subjects;
using System.Threading.Channels;

namespace Omnikeeper.Utils
{
    public class LogLine
    {
        public string? Category { get; set; }
        public LogLevel LogLevel { get; set; }
        public string? Message { get; set; }
    }

    public class ReactiveLogReceiver
    {
        private readonly Subject<LogLine> Subject = new Subject<LogLine>();

        public void WriteLine(LogLine line)
        {
            Subject.OnNext(line);
        }

        public IObservable<LogLine> Observable => Subject;
    }

    public class SignalRHubLogging : Hub
    {
        private readonly ReactiveLogReceiver lr;

        public SignalRHubLogging(ReactiveLogReceiver lr)
        {
            this.lr = lr;
        }

        public ChannelReader<LogLine> StreamLogs()
        {
            return lr.Observable.AsChannelReader(10);
        }
    }

    public static class ObservableExtensions
    {
        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> observable, int? maxBufferSize = null)
        {
            // This sample shows adapting an observable to a ChannelReader without 
            // back pressure, if the connection is slower than the producer, memory will
            // start to increase.

            // If the channel is bounded, TryWrite will return false and effectively
            // drop items.

            // The other alternative is to use a bounded channel, and when the limit is reached
            // block on WaitToWriteAsync. This will block a thread pool thread and isn't recommended and isn't shown here.
            var channel = maxBufferSize != null ? Channel.CreateBounded<T>(maxBufferSize.Value) : Channel.CreateUnbounded<T>();

            var disposable = observable.Subscribe(
                                value => channel.Writer.TryWrite(value),
                                error => channel.Writer.TryComplete(error),
                                () => channel.Writer.TryComplete());

            // Complete the subscription on the reader completing
            channel.Reader.Completion.ContinueWith(task => disposable.Dispose());

            return channel.Reader;
        }
    }
}
