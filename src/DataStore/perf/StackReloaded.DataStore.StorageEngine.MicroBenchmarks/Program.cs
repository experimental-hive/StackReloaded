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
        static void Main()
        {
            var benchmarkJobAttribute = new BenchmarkJobAttribute();
            var config = benchmarkJobAttribute.Config;

            var runningSummaries = Runs(config);
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

        private static IEnumerable<Summary> Runs(IConfig config)
        {
            Type singleBenchmarkType = null;
            //Type singleBenchmarkType = typeof(Pages.PageBenchmarks.PageDeleteRawBytesBenchmarks);

            if (singleBenchmarkType != null)
            {
                yield return BenchmarkRunner.Run(singleBenchmarkType, config);
                yield break;
            }

            yield return BenchmarkRunner.Run<Collections.BPlusTreeBenchmarks.BPlusTreeInsertBenchmarks>(config);
            yield return BenchmarkRunner.Run<Collections.BPlusTreeBenchmarks.BPlusTreeDeleteBenchmarks>(config);

            yield return BenchmarkRunner.Run<Pages.PageBenchmarks.PageInsertRawBytesBenchmarks>(config);
            yield return BenchmarkRunner.Run<Pages.PageBenchmarks.PageDeleteRawBytesBenchmarks>(config);
        }
    }
}
