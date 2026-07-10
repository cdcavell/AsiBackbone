using System.Reflection;
using AsiBackbone.AspNetCore.Endpoints;
using Xunit;
namespace AsiBackbone.AspNetCore.Tests.Endpoints;
public sealed class ConfiguredDiagnosticsRedactionTests
{
    [Fact]
    public void ConfiguredKeyIsRedacted()
    {
        var options = new AsiBackboneEndpointGovernanceOptions();
        options.DevelopmentDiagnosticsRedactedMetadataKeys.Add("tenant.reference");
        Assert.True(Redacts(options, "TENANT.REFERENCE"));
    }
    private static bool Redacts(AsiBackboneEndpointGovernanceOptions options, string key) => (bool)typeof(AsiBackboneEndpointGovernanceDevelopmentDiagnostics).GetMethod("ShouldRedactMetadataValue", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [options, key])!;
}