using System;
using System.Collections.Generic;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var argsList = new List<string>(args);

            var benchmarkJobAttribute = new BenchmarkJobAttribute();
            var config = benchmarkJobAttribute.Config;

            var runningSummaries = Runs(argsList, config);
            var collectedSummaries = new List<Summary>();

            foreach (var summary in runningSummaries)
            {
                collectedSummaries.Add(summary);

                //var logger = ConsoleLogger.Default;
                //MarkdownExporter.Console.ExportToLog(summary, logger);
                //ConclusionHelper.Print(logger, summary.BenchmarksCases.First().Config.GetCompositeAnalyser().Analyse(summary).ToList());
            }

            // Summaries opnieuw tonen na alle benchmarks zijn uitgevoerd.
            foreach (var summary in collectedSummaries)
            {
                var logger = ConsoleLogger.Default;
                QuickSummaryExporter.Console.ExportToLog(summary, logger);
            }
        }

        private static IEnumerable<Summary> Runs(List<string> argsList, IConfig config, Type singleBenchmarkType = null)
        {
            if (singleBenchmarkType != null)
            {
                var singleSummary = BenchmarkRunner.Run(singleBenchmarkType, config);
                return new[] { singleSummary };
            }

            return BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(argsList.ToArray(), config: config);
        }
    }
}
