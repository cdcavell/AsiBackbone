using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace AsiBackbone.Analyzers.Tests;

public sealed class GovernanceArtifactPersistenceAnalyzerTests
{
    [Fact]
    public async Task DiscardedGovernanceDecisionReportsASIB001()
    {
        string source = SourceWithBody("GovernanceDecision.Allow();");

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(GovernanceArtifactPersistenceAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task DiscardAssignmentReportsASIB001()
    {
        string source = SourceWithBody("_ = GovernanceDecision.Allow();");

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(GovernanceArtifactPersistenceAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task StoredGovernanceDecisionDoesNotReport()
    {
        string source = SourceWithBody("GovernanceDecision decision = GovernanceDecision.Allow(); _ = decision;");

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReturnedGovernanceDecisionDoesNotReport()
    {
        string source = """
            using AsiBackbone.Core.Decisions;

            public static class Sample
            {
                public static GovernanceDecision Create()
                {
                    return GovernanceDecision.Allow();
                }
            }

            namespace AsiBackbone.Core.Decisions
            {
                public sealed class GovernanceDecision
                {
                    public static GovernanceDecision Allow() => new();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AwaitedGovernanceDecisionReportsASIB001()
    {
        string source = """
            using System.Threading.Tasks;
            using AsiBackbone.Core.Decisions;

            public static class Sample
            {
                public static async Task ExecuteAsync()
                {
                    await Task.FromResult(GovernanceDecision.Allow());
                }
            }

            namespace AsiBackbone.Core.Decisions
            {
                public sealed class GovernanceDecision
                {
                    public static GovernanceDecision Allow() => new();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(GovernanceArtifactPersistenceAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task OperationResultOfGovernanceDecisionReportsASIB001()
    {
        string source = """
            using AsiBackbone.Core.Decisions;
            using AsiBackbone.Core.Results;

            public static class Sample
            {
                public static void Execute()
                {
                    OperationResult<GovernanceDecision>.Success(GovernanceDecision.Allow());
                }
            }

            namespace AsiBackbone.Core.Decisions
            {
                public sealed class GovernanceDecision
                {
                    public static GovernanceDecision Allow() => new();
                }
            }

            namespace AsiBackbone.Core.Results
            {
                public sealed class OperationResult<T>
                {
                    public static OperationResult<T> Success(T value) => new();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(GovernanceArtifactPersistenceAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task HostMarkerAttributeSuppressesASIB001()
    {
        string source = """
            using System;
            using AsiBackbone.Core.Decisions;

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
            internal sealed class AsiBackbonePersistenceHandledAttribute : Attribute;

            public static class Sample
            {
                [AsiBackbonePersistenceHandled]
                public static void Execute()
                {
                    GovernanceDecision.Allow();
                }
            }

            namespace AsiBackbone.Core.Decisions
            {
                public sealed class GovernanceDecision
                {
                    public static GovernanceDecision Allow() => new();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Assert.Empty(diagnostics);
    }

    private static string SourceWithBody(string body)
    {
        return $$"""
            using AsiBackbone.Core.Decisions;

            public static class Sample
            {
                public static void Execute()
                {
                    {{body}}
                }
            }

            namespace AsiBackbone.Core.Decisions
            {
                public sealed class GovernanceDecision
                {
                    public static GovernanceDecision Allow() => new();
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
            "AnalyzerTestAssembly",
            [syntaxTree],
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Diagnostic[] compilerErrors = [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];

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
