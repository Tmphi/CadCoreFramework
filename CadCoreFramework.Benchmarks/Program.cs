using BenchmarkDotNet.Running;

namespace CadCoreFramework.Benchmarks;

/// <summary>
/// Entry point — delegates to BenchmarkSwitcher so individual benchmarks
/// can be selected from the command line:
///
///   dotnet run -c Release -- --filter *SelectionFilterBuilder*
///
/// Without filter: BDN prompts interactively which classes to run.
/// </summary>
public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
