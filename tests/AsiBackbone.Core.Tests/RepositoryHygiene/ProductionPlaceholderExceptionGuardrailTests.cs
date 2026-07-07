using System.Text.RegularExpressions;
using Xunit;

namespace AsiBackbone.Core.Tests.RepositoryHygiene;

/// <summary>
/// Repository-level regression checks that keep production source paths free of placeholder exception traps.
/// </summary>
public sealed partial class ProductionPlaceholderExceptionGuardrailTests
{
    private static readonly Regex PlaceholderExceptionPattern = MyRegex();

    /// <summary>
    /// Verifies production library source files do not contain accidental NotImplementedException placeholders.
    /// </summary>
    [Fact]
    public void ProductionSourceDoesNotContainNotImplementedExceptionPlaceholders()
    {
        string repositoryRoot = FindRepositoryRoot();
        string sourceRoot = Path.Combine(repositoryRoot, "src");

        Assert.True(Directory.Exists(sourceRoot), $"Source root was not found: {sourceRoot}");

        string[] violations = [.. Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionSourceFile)
            .Where(ContainsPlaceholderException)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Production source files must not contain NotImplementedException placeholders. " +
            "Use a domain-specific exception, NotSupportedException, InvalidOperationException, a fail-closed result, " +
            "or document an explicit non-production exception allowance. Violations: " +
            string.Join(", ", violations));
    }

    private static bool ContainsPlaceholderException(string path)
    {
        string content = File.ReadAllText(path);

        return PlaceholderExceptionPattern.IsMatch(content);
    }

    private static bool IsProductionSourceFile(string path)
    {
        string normalizedPath = path.Replace(Path.DirectorySeparatorChar, '/');

        return !normalizedPath.Contains("/bin/", StringComparison.Ordinal) &&
               !normalizedPath.Contains("/obj/", StringComparison.Ordinal) &&
               !normalizedPath.Contains("/AsiBackbone.Templates/templates/", StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string solutionPath = Path.Combine(directory.FullName, "AsiBackbone.slnx");

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root containing AsiBackbone.slnx.");
    }

    [GeneratedRegex(@"\bNotImplementedException\s*\(", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
