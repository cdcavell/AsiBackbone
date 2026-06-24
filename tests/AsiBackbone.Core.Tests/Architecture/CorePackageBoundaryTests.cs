using System.Xml.Linq;
using Xunit;

namespace AsiBackbone.Core.Tests.Architecture;

/// <summary>
/// Architecture-boundary tests for the stable Core package contract.
/// </summary>
public sealed class CorePackageBoundaryTests
{
    private static readonly string[] ForbiddenCoreDependencyPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.SemanticKernel",
        "Azure.",
        "AWS.",
        "Amazon.",
        "Google.Cloud",
        "OpenAI",
        "Anthropic",
        "System.Device",
        "Iot.",
        "ROS",
        "Robotics"
    ];

    /// <summary>
    /// Verifies that Core remains framework-neutral and does not depend on integration or provider packages.
    /// </summary>
    [Fact]
    public void CoreProjectDoesNotReferenceIntegrationOrProviderDependencies()
    {
        XDocument project = LoadProject("src/AsiBackbone.Core/AsiBackbone.Core.csproj");

        string[] projectReferences = GetIncludes(project, "ProjectReference");
        Assert.Empty(projectReferences);

        string[] packageReferences = GetIncludes(project, "PackageReference");
        string[] forbiddenReferences = [.. packageReferences.Where(IsForbiddenCoreDependency)];

        Assert.Empty(forbiddenReferences);
    }

    /// <summary>
    /// Verifies package dependency direction for the stable package family.
    /// </summary>
    [Fact]
    public void StablePackagesUseOnlyApprovedProjectReferences()
    {
        var expectedReferencesByProject = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["src/AsiBackbone.DependencyInjection/AsiBackbone.DependencyInjection.csproj"] = [],
            ["src/AsiBackbone.Storage.InMemory/AsiBackbone.Storage.InMemory.csproj"] =
            [
                "../AsiBackbone.Core/AsiBackbone.Core.csproj",
                "../AsiBackbone.DependencyInjection/AsiBackbone.DependencyInjection.csproj"
            ],
            ["src/AsiBackbone.EntityFrameworkCore/AsiBackbone.EntityFrameworkCore.csproj"] =
            [
                "../AsiBackbone.Core/AsiBackbone.Core.csproj",
                "../AsiBackbone.DependencyInjection/AsiBackbone.DependencyInjection.csproj"
            ],
            ["src/AsiBackbone.AspNetCore/AsiBackbone.AspNetCore.csproj"] =
            [
                "../AsiBackbone.Core/AsiBackbone.Core.csproj",
                "../AsiBackbone.DependencyInjection/AsiBackbone.DependencyInjection.csproj"
            ],
            ["src/AsiBackbone.OpenTelemetry/AsiBackbone.OpenTelemetry.csproj"] =
            [
                "../AsiBackbone.Core/AsiBackbone.Core.csproj",
                "../AsiBackbone.DependencyInjection/AsiBackbone.DependencyInjection.csproj"
            ],
            ["src/AsiBackbone.Signing.LocalDevelopment/AsiBackbone.Signing.LocalDevelopment.csproj"] =
            [
                "../AsiBackbone.Core/AsiBackbone.Core.csproj",
                "../AsiBackbone.DependencyInjection/AsiBackbone.DependencyInjection.csproj"
            ],
            ["src/AsiBackbone.Signing.ManagedKey/AsiBackbone.Signing.ManagedKey.csproj"] =
            [
                "../AsiBackbone.Core/AsiBackbone.Core.csproj",
                "../AsiBackbone.DependencyInjection/AsiBackbone.DependencyInjection.csproj"
            ]
        };

        foreach (KeyValuePair<string, string[]> expected in expectedReferencesByProject)
        {
            XDocument project = LoadProject(expected.Key);
            string[] actualReferences = [.. GetIncludes(project, "ProjectReference")
                .Select(NormalizePath)
                .Order(StringComparer.Ordinal)];
            string[] expectedReferences = [.. expected.Value
                .Select(NormalizePath)
                .Order(StringComparer.Ordinal)];

            Assert.Equal(expectedReferences, actualReferences);
        }
    }

    private static bool IsForbiddenCoreDependency(string packageReference)
    {
        return ForbiddenCoreDependencyPrefixes.Any(prefix =>
            packageReference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static XDocument LoadProject(string relativePath)
    {
        DirectoryInfo repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(repositoryRoot.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));

        return XDocument.Load(projectPath);
    }

    private static string[] GetIncludes(XDocument project, string elementName)
    {
        return [.. project
            .Descendants(elementName)
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)];
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AsiBackbone.slnx")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root from the test output directory.");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
