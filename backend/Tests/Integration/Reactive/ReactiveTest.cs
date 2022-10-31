using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Omnikeeper.Runners.Reactive;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Integration.Reactive
{
    public class ReactiveTest : DIServicedTestBase
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            //builder.RegisterType<CLBDummy>().As<IComputeLayerBrain>().SingleInstance();

            IServiceCollection services = new ServiceCollection();
            services.AddHostedService<ReactiveHostedService>();
            builder.Populate(services);

            builder.Register<ILoggerFactory>(t => LoggerFactory.Create(builder => builder.AddConsole())).SingleInstance();
        }

        //[Test]
        //[Explicit]
        //public async Task Test()
        //{
        //    var service = (ServiceProvider.GetRequiredService<IHostedService>() as ReactiveHostedService)!;

        //    await service.StartAsync(CancellationToken.None);

        //    await Task.Delay(15000);

        //    await service.StopAsync(CancellationToken.None);
        //}

        // simple timer class, just here so we have a reference typed counter that we can pass around
        public class Counter
        {
            public int Value { get; set; }
        }

        // data that is moving through observable pipeline, must be disposed at the end
        public class RunData : IDisposable
        {
            private readonly Counter counter;

            public RunData(Counter counter)
            {
                this.counter = counter;

                Console.WriteLine("Created");
                counter.Value++;
            }

            public void Dispose()
            {
                Console.WriteLine("Dispose called");
                counter.Value--;
            }
        }

        [Test]
        [Explicit]
        public async Task TestHigherOrderExceptionHandling()
        {
            var counter = new Counter();

            var useHigherOrderExceptionHandling = true; // test succeeds when false, fails when true

            //var obs = Observable.Create<RunData>(async (o) =>
            //{
            //    await Task.Delay(100); // just here to justify the async nature
            //    o.OnNext(new RunData(counter)); // produce a new RunData object, must be disposed later!
            //    o.OnCompleted();
            //    return Disposable.Empty;
            //})
            var semaphoreSlim = new SemaphoreSlim(1, 1);
            var obs = Observable.Create<RunData>(async (o) =>
            {
                Console.WriteLine("Waiting for semaphore");
                await semaphoreSlim.WaitAsync();
                o.OnNext(new RunData(counter));
                //await Task.Delay(1000);
                o.OnCompleted();
                return Disposable.Empty;
            })
                .Concat(Observable.Empty<RunData>().Delay(TimeSpan.FromMilliseconds(10)))
                .Repeat(3) // Resubscribe indefinitely after source completes
                .Publish().RefCount() // see http://northhorizon.net/2011/sharing-in-rx/
                ;

            //var obs = Observable.Interval(TimeSpan.FromSeconds(1))
            //    .Select(x => Observable.FromAsync(() => {
            //        return Task.FromResult(1);
            //    }))
            //    .Merge(1)
            //    .Select(x => new RunData(counter))
            //    //.Select(_ =>
            //    //{
            //    //    return ;
            //    //})
            //    .Publish().RefCount() // see http://northhorizon.net/2011/sharing-in-rx/
            //    //.ObserveLatestOn(Scheduler.Default)
            //    ;

            // transforms the stream, exceptions might be thrown inside of stream, would like to catch them and handle them appropriately
            IObservable<(bool result, RunData runData)> TransformRunDataToResult(IObservable<RunData> obs)
            {
                return obs.Select(rd => Observable.FromAsync(async ct =>
                {
                    await Task.Delay(100);
                    return (result: true, runData: rd);
                })).Concat(); // very simple example, real-world transformation would be more complex
            }

            IObservable<(bool result, RunData runData)> safeObs;
            if (useHigherOrderExceptionHandling)
            {
                //safeObs = obs.Select(rd =>
                //    TransformRunDataToResult(obs)
                //    .Catch((Exception e) => // try to catch any exception occurring within the stream, return a new tuple with result: false if that happens
                //    {
                //        return (Observable.Return((result: false, runData: rd)));
                //    })
                //).Concat();

                safeObs = obs.Publish(_obs => _obs
                    .Select(rd => TransformRunDataToResult(_obs)
                        .Catch((Exception e) => Observable.Return((result: false, runData: rd)))
                    ))
                    .Concat();
            }
            else
            {
                safeObs = TransformRunDataToResult(obs);
            }

            safeObs.Select(t =>
                Observable.FromAsync(async () =>
                {
                    Console.WriteLine($"Calling async");
                    var (result, runData) = t;
                    try
                    {
                        await Task.Delay(100); // just here to justify the async nature

                        Console.WriteLine($"Result: {result}");
                    }
                    finally
                    {
                        t.runData.Dispose(); // dispose RunData instance that was created by the observable above
                        Console.WriteLine("Releasing semaphore");
                        semaphoreSlim.Release();
                    }
                }))
                .Concat()
                .Subscribe();

            await Task.Delay(3000); // give observable enough time to produce a few items

            Assert.AreEqual(0, counter.Value);
        }

        [Test]
        [Explicit]
        public async Task TestHigherOrderExceptionHandling2()
        {
            var counter = new Counter();
            var useHigherOrderExceptionHandling = true; // test succeeds when false, fails when true

            var obs = Observable.Create<RunData>(o =>
            {
                o.OnNext(new RunData(counter)); // produce a new RunData object, must be disposed later!
                o.OnCompleted();
                return Disposable.Empty;
            })
                .Concat(Observable.Empty<RunData>().Delay(TimeSpan.FromSeconds(1)))
                .Repeat(3) // Resubscribe two more times after source completes
                .Publish().RefCount() // see http://northhorizon.net/2011/sharing-in-rx/
                ;

            // transforms the stream, exceptions might be thrown inside of stream, I would like to catch them and handle them appropriately
            IObservable<(bool result, RunData runData)> TransformRunDataToResult(IObservable<RunData> obs)
            {
                return obs.Select(rd =>
                {
                    // simple toy example, could throw exception here in practice
                    // throw new Exception();
                    return (result: true, runData: rd);
                });
            }

            IObservable<(bool result, RunData runData)> safeObs;
            if (useHigherOrderExceptionHandling)
            {
                safeObs = obs.Publish(_obs => _obs
                    .Select(rd => TransformRunDataToResult(_obs)
                        .Catch((Exception e) => Observable.Return((result: false, runData: rd)))
                    ))
                    .Concat();
            }
            else
            {
                safeObs = TransformRunDataToResult(obs);
            }

            safeObs.Subscribe(
                t =>
                {
                    var (result, runData) = t;
                    try
                    {
                        Console.WriteLine($"Result: {result}");
                    }
                    finally
                    {
                        t.runData.Dispose(); // dispose RunData instance that was created by the observable above
                    }
                });

            await Task.Delay(4000); // give observable enough time to produce a few items

            Assert.AreEqual(0, counter.Value);
        }

        [Test]
        [Explicit]
        public async Task TestDo()
        {
            var counter = new Counter();

            var obs = Observable.Create<RunData>(async (o) =>
            {
                await Task.Delay(100); // just here to justify the async nature

                Console.WriteLine("Calling OnNext");
                o.OnNext(new RunData(counter)); // produce a new RunData object, must be disposed later!

                Console.WriteLine("Calling onComplete");
                o.OnCompleted();

                return Disposable.Empty;
            })
                .Concat(Observable.Empty<RunData>().Delay(TimeSpan.FromSeconds(1)))
                .Repeat() // Resubscribe indefinitely after source completes
                .Publish().RefCount() // see http://northhorizon.net/2011/sharing-in-rx/
                ;

            var safeObs = obs.Select(rd => (result: true, runData: rd));

            //safeObs.Do(async t =>
            safeObs.Select(t =>
                Observable.FromAsync(async () =>
                {
                    Console.WriteLine($"Calling async");
                    var (result, runData) = t;
                    try
                    {
                        await Task.Delay(100); // just here to justify the async nature

                        Console.WriteLine($"Result: {result}");
                    }
                    finally
                    {
                        t.runData.Dispose(); // dispose RunData instance that was created by the observable above
                    }
                }))
                .Concat()
                .Subscribe();


            await Task.Delay(5000); // give observable enough time to produce a few items

            Assert.AreEqual(0, counter.Value);
        }
    }
}
