# run tests from commandline
~~~bash 
dotnet run --project ./PerfTests -c Release -- --job short --runtimes net7.0 --filter * --exporters json --strategy Monitoring
~~~

~~~bash
dotnet run --project ./PerfTests -c Release -- --job short --runtimes net7.0 --filter PerfTests.GetMergedAttributesTest.GetMergedAttributes --exporters json --strategy Monitoring
~~~

~~~bash
dotnet run --project ./PerfTests -c Release -- --job short --runtimes net7.0 --filter PerfTests.BulkReplaceAttributesTest.BulkReplaceAttributes --exporters json --strategy Monitoring
~~~