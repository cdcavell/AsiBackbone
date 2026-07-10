using System.Reflection;
using AsiBackbone.AspNetCore.Endpoints;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.Endpoints;

public sealed class SensitiveNameDiagnosticsRedactionTests
{
    [Theory]
    [InlineData("service.apiToken")]
    [InlineData("database.password")]
    [InlineData("clientSecretReference")]
    [InlineData("authorization.context")]
    public void SensitiveNameKeyIsRedacted(string key)
    {
        var options = new AsiBackboneEndpointGovernanceOptions();
        Assert.True(Redacts(options, key));
    }

    private static bool Redacts(AsiBackboneEndpointGovernanceOptions options, string key) =>
        (bool)typeof(AsiBackboneEndpointGovernanceDevelopmentDiagnostics)
            .GetMethod("ShouldRedactMetadataValue", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [options, key])!;
}
