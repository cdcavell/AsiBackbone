using CDCavell.AsiBackbone.Core.Serialization;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Signing;

public sealed class CanonicalPayloadJsonBranchTests
{
    [Fact]
    public void CreateSerializesSupportedPrimitiveDictionaryAndArrayValues()
    {
        var payload = CanonicalPayload.Create(
            CanonicalArtifactTypes.AuditLedgerRecord,
            "record-1",
            AsiBackboneSchemaVersions.StableArtifactsV1,
            CanonicalPayloadOptions.DefaultCanonicalizationVersion,
            new Dictionary<string, object?>
            {
                ["nullValue"] = null,
                ["stringValue"] = "alpha",
                ["boolValue"] = true,
                ["intValue"] = 7,
                ["longValue"] = 8L,
                ["doubleValue"] = 0.25d,
                ["stringDictionary"] = new Dictionary<string, string>
                {
                    ["beta"] = "2",
                    ["alpha"] = "1"
                },
                ["stringArray"] = new[] { "beta", "alpha" },
                ["objectArray"] = new object?[] { "gamma", 9, false, null },
                ["nested"] = new Dictionary<string, object?>
                {
                    ["child"] = "value"
                }
            });

        Assert.Contains("\"boolValue\":true", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"doubleValue\":0.25", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"intValue\":7", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"longValue\":8", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"nullValue\":null", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"objectArray\":[\"gamma\",9,false,null]", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"stringArray\":[\"beta\",\"alpha\"]", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"stringDictionary\":{\"alpha\":\"1\",\"beta\":\"2\"}", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"nested\":{\"child\":\"value\"}", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.NotEmpty(payload.ToUtf8Bytes());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CreateRejectsNonFiniteDoubleValues(double value)
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CanonicalPayload.Create(
                CanonicalArtifactTypes.AuditLedgerRecord,
                "record-1",
                AsiBackboneSchemaVersions.StableArtifactsV1,
                CanonicalPayloadOptions.DefaultCanonicalizationVersion,
                new Dictionary<string, object?>
                {
                    ["number"] = value
                }));
    }

    [Fact]
    public void CreateRejectsUnsupportedContentValueType()
    {
        _ = Assert.Throws<NotSupportedException>(() =>
            CanonicalPayload.Create(
                CanonicalArtifactTypes.AuditLedgerRecord,
                "record-1",
                AsiBackboneSchemaVersions.StableArtifactsV1,
                CanonicalPayloadOptions.DefaultCanonicalizationVersion,
                new Dictionary<string, object?>
                {
                    ["unsupported"] = DateTimeOffset.UtcNow
                }));
    }
}
