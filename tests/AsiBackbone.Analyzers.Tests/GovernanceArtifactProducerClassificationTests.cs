using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace AsiBackbone.Analyzers.Tests;

/// <summary>
/// Focused classification coverage for artifact-producing operations recognized by
/// <see cref="GovernanceArtifactPersistenceAnalyzer"/>.
/// </summary>
public sealed class GovernanceArtifactProducerClassificationTests
{
    /// <summary>
    /// Verifies direct invocations, including overloaded, generic, and extension methods,
    /// are recognized when they return a governance artifact.
    /// </summary>
    [Theory]
    [InlineData("GovernanceDecision.Create();")]
    [InlineData("GovernanceDecision.Create(1);")]
    [InlineData("GovernanceDecision.Create<string>();")]
    [InlineData("1.CreateDecision();")]
    public async Task InvocationProducersReportDiagnostic(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(SourceWithBody(
            statement,
            additionalMembers: """
                public static GovernanceDecision Create() => new();
                public static GovernanceDecision Create(int value) => new();
                public static GovernanceDecision Create<T>() => new();
                """,
            additionalSource: """
                public static class DecisionExtensions
                {
                    public static AsiBackbone.Core.Decisions.GovernanceDecision CreateDecision(this int value) => new();
                }
                """));

        AssertDiagnostic(diagnostics);
    }

    /// <summary>
    /// Verifies object creation and property access are recognized as producer operations.
    /// </summary>
    [Theory]
    [InlineData("new GovernanceDecision();")]
    [InlineData("GovernanceDecision.Current;")]
    public async Task ObjectAndPropertyProducersReportDiagnostic(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(SourceWithBody(
            statement,
            additionalMembers: "public static GovernanceDecision Current => new();"));

        AssertDiagnostic(diagnostics);
    }

    /// <summary>
    /// Verifies awaited and converted producer operations remain recognized.
    /// </summary>
    [Fact]
    public async Task AwaitedAndConvertedProducersReportDiagnostic()
    {
        string source = """
            using System.Threading.Tasks;
            using AsiBackbone.Core.Decisions;

            public static class Sample
            {
                public static async Task ExecuteAsync()
                {
                    await Task.FromResult((GovernanceDecision)new DecisionSource());
                }
            }

            public sealed class DecisionSource
            {
                public static implicit operator GovernanceDecision(DecisionSource source) => new();
            }

            namespace AsiBackbone.Core.Decisions
            {
                public sealed class GovernanceDecision
                {
                    public GovernanceDecision()
                    {
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        AssertDiagnostic(diagnostics);
    }

    /// <summary>
    /// Verifies conditional and coalesce expressions report when either branch produces an artifact.
    /// </summary>
    [Theory]
    [InlineData("flag ? GovernanceDecision.Create() : existing;")]
    [InlineData("flag ? existing : GovernanceDecision.Create();")]
    [InlineData("existing ?? GovernanceDecision.Create();")]
    public async Task CompositeProducerExpressionsReportDiagnostic(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(SourceWithBody(
            statement,
            prelude: "bool flag = true; GovernanceDecision? existing = null;",
            additionalMembers: "public static GovernanceDecision Create() => new();"));

        AssertDiagnostic(diagnostics);
    }

    /// <summary>
    /// Verifies non-producing references, unsupported expressions, dynamic calls, and near-match types do not report.
    /// </summary>
    [Theory]
    [InlineData("_ = existing;")]
    [InlineData("_ = default(GovernanceDecision);")]
    [InlineData("_ = flag ? existing : existing;")]
    [InlineData("dynamic producer = new object(); producer.Create();")]
    public async Task UnsupportedOrUnresolvedOperationsDoNotReport(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(SourceWithBody(
            statement,
            prelude: "bool flag = true; GovernanceDecision existing = new();"));

        Assert.Empty(diagnostics);
    }

    /// <summary>
    /// Verifies a similarly named type outside the recognized namespace is not classified as a governance artifact.
    /// </summary>
    [Fact]
    public async Task NearMatchArtifactTypeDoesNotReport()
    {
        string source = """
            public static class Sample
            {
                public static void Execute()
                {
                    Similar.Decisions.GovernanceDecision.Create();
                }
            }

            namespace Similar.Decisions
            {
                public sealed class GovernanceDecision
                {
                    public static GovernanceDecision Create() => new();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Assert.Empty(diagnostics);
    }

    private static void AssertDiagnostic(ImmutableArray<Diagnostic> diagnostics)
    {
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(GovernanceArtifactPersistenceAnalyzer.DiagnosticId, diagnostic.Id);
    }

    private static string SourceWithBody(
        string statement,
        string prelude = "",
        string additionalMembers = "",
        string additionalSource = "")
    {
        return $$"""
            using AsiBackbone.Core.Decisions;

            public static class Sample
            {
                public static void Execute()
                {
                    {{prelude}}
                    {{statement}}
                }
            }

            {{additionalSource}}

            namespace AsiBackbone.Core.Decisions
            {
                public sealed class GovernanceDecision
                {
                    public GovernanceDecision()
                    {
                    }

                    {{additionalMembers}}
                }
            }
            """;
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            "ProducerClassificationTestAssembly",
            [syntaxTree],
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Diagnostic[] compilerErrors = [.. compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
        Assert.Empty(compilerErrors);

        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            [new GovernanceArtifactPersistenceAnalyzer()]);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static PortableExecutableReference[] GetMetadataReferences()
    {
        string trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies were not available for analyzer test compilation.");

        return [.. trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => MetadataReference.CreateFromFile(path))];
    }
}
