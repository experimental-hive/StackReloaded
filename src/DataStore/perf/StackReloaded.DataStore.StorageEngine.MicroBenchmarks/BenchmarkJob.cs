using System;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.CsProj;
using Perfolizer.Horology;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class BenchmarkJobAttribute : Attribute, IConfigSource
    {
        public BenchmarkJobAttribute()
        {
            var job = new Job()
                .WithLaunchCount(1)
                //.WithIterationTime(TimeInterval.FromMilliseconds(100))
                //.WithWarmupCount(3)
                //.WithIterationCount(3)
                .WithToolchain(CsProjCoreToolchain.NetCoreApp31);

            var config = new ManualConfig();
            config.AddColumn(StatisticColumn.Min, StatisticColumn.Max);
            config.AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
            //config.AddExporter(DefaultConfig.Instance.GetExporters().ToArray());
            config.AddDiagnoser(DefaultConfig.Instance.GetDiagnosers().ToArray());
            config.AddAnalyser(DefaultConfig.Instance.GetAnalysers().Where(x => x as MultimodalDistributionAnalyzer == null).ToArray());
            config.AddJob(job);
            config.AddValidator(DefaultConfig.Instance.GetValidators().ToArray());
            //config.AddLogger(NullLogger.Instance);
            config.AddLogger(ConsoleLogger.Default);
            config.AddDiagnoser(MemoryDiagnoser.Default);
            config.UnionRule = ConfigUnionRule.AlwaysUseGlobal; // Overriding the default

            //var config = ManualConfig.CreateEmpty()
            //    //.With(MemoryDiagnoser.Default)
            //    .With(job.With(CsProjCoreToolchain.NetCoreApp22));

            this.Config = config;
        }

        public IConfig Config { get; }
    }
}
