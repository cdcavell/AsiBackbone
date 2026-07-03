using BenchmarkDotNet.Running;

namespace AsiBackbone.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        _ = BenchmarkSwitcher.FromAssembly(typeof(AsiBackboneHotPathBenchmarks).Assembly).Run(args);
    }
}
