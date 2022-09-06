# run tests from commandline
~~~bash 
dotnet run --project ./PerfTests -c Release -- --job short --runtimes net6.0 --filter * --exporters json --strategy Monitoring --iterationCount 50
~~~

~~~bash
dotnet run --project ./PerfTests -c Release -- --job short --runtimes net6.0 --filter PerfTests.GetMergedAttributesTest.GetMergedAttributes --exporters json --strategy Monitoring --iterationCount 50
~~~

~~~bash
dotnet run --project ./PerfTests -c Release -- --job short --runtimes net6.0 --filter PerfTests.BulkReplaceAttributesTest.BulkReplaceAttributes --exporters json --strategy Monitoring --iterationCount 10
~~~