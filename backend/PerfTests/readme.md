# run tests from commandline
~~~bash 
dotnet run --project ./PerfTests -c Release -- --job short --runtimes netcoreapp31 --filter * --exporters json
~~~