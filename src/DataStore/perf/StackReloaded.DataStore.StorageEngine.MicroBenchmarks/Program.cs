using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Text;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var argsList = new List<string>(args);

            var summaries = BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(argsList.ToArray());
        }
    }
}
