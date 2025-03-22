using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace AdvanceFileUpload.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = DefaultConfig.Instance
            .AddJob(Job
         .MediumRun
         .WithLaunchCount(1)
         .WithToolchain(InProcessNoEmitToolchain.Instance));
            BenchmarkRunner.Run<FileSplitterBenchmarks>(config);
        }
    }
}
