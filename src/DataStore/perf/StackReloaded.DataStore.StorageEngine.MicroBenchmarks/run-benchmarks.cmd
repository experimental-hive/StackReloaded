@echo off
dotnet run -c Release -- --filter * --runtimes netcoreapp3.1 --warmupCount 1 --minIterationCount 2 --maxIterationCount 5
pause