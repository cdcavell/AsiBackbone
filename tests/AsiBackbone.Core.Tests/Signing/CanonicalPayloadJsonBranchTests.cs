using AsiBackbone.Core.Serialization;
using AsiBackbone.Core.Signing;
using Xunit;

namespace AsiBackbone.Core.Tests.Signing;

/// <summary>
/// Tests for the <see cref="CanonicalPayload"/> class, specifically focusing on the JSON serialization of supported primitive dictionary and array values, as well as handling of unsupported content value types and non-finite double values.
/// </summary>
public sealed class CanonicalPayloadJsonBranchTests
{
    private static readonly string[] content = ["beta", "alpha"];

    /// <summary>
    /// Tests that the <see cref="CanonicalPayload.Create"/> method correctly serializes supported primitive dictionary and array values into canonical JSON format. The test verifies that the resulting JSON string contains the expected key-value pairs and that the payload can be converted to UTF-8 bytes without errors.
    /// </summary>
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
                ["stringArray"] = content,
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

    /// <summary>
    /// Tests that the <see cref="CanonicalPayload.Create"/> method throws an <see cref="ArgumentOutOfRangeException"/> when attempting to create a payload with non-finite double values (NaN, PositiveInfinity, NegativeInfinity). The test uses the [Theory] attribute to run the test for each of the specified non-finite double values.
    /// </summary>
    /// <param name="value">
    /// The non-finite double value to test (NaN, PositiveInfinity, or NegativeInfinity).
    /// </param>
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

    /// <summary>
    /// Tests that the <see cref="CanonicalPayload.Create"/> method throws a <see cref="NotSupportedException"/> when attempting to create a payload with an unsupported content value type (in this case, a DateTimeOffset). The test verifies that the exception is thrown as expected when an unsupported type is included in the content dictionary.
    /// </summary>
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
