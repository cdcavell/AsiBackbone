using System.Collections;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests;

internal static class ReadOnlyMetadataAssert
{
    public static void CannotMutateThroughCasts(IReadOnlyDictionary<string, string> metadata)
    {
        bool genericDictionaryCastWasAvailable = AssertCannotMutateThroughGenericDictionary(metadata);
        bool nonGenericDictionaryCastWasAvailable = AssertCannotMutateThroughNonGenericDictionary(metadata);

        // This assertion is intentional. The production metadata implementation currently uses
        // ReadOnlyDictionary<string, string>, which still exposes dictionary cast surfaces but throws
        // on mutation attempts. Requiring at least one cast surface prevents this helper from passing
        // without exercising an actual mutation path. If metadata later switches to a type that only
        // implements IReadOnlyDictionary<string, string>, this assertion should be revisited because
        // that would be a stricter immutable shape with no cast attack surface to test.
        Assert.True(
            genericDictionaryCastWasAvailable || nonGenericDictionaryCastWasAvailable,
            "Metadata did not expose a dictionary cast surface to test.");
    }

    private static bool AssertCannotMutateThroughGenericDictionary(
        IReadOnlyDictionary<string, string> metadata)
    {
        const string setKey = "__mutation_set__";
        const string addKey = "__mutation_add__";

        if (metadata is not IDictionary<string, string> dictionary)
        {
            return false;
        }

        _ = Assert.Throws<NotSupportedException>(() => dictionary[setKey] = "blocked");
        _ = Assert.Throws<NotSupportedException>(() => dictionary.Add(addKey, "blocked"));

        Assert.False(metadata.ContainsKey(setKey));
        Assert.False(metadata.ContainsKey(addKey));

        return true;
    }

    private static bool AssertCannotMutateThroughNonGenericDictionary(
        IReadOnlyDictionary<string, string> metadata)
    {
        const string setKey = "__non_generic_mutation_set__";
        const string addKey = "__non_generic_mutation_add__";

        if (metadata is not IDictionary dictionary)
        {
            return false;
        }

        _ = Assert.Throws<NotSupportedException>(() => dictionary[setKey] = "blocked");
        _ = Assert.Throws<NotSupportedException>(() => dictionary.Add(addKey, "blocked"));

        Assert.False(metadata.ContainsKey(setKey));
        Assert.False(metadata.ContainsKey(addKey));

        return true;
    }
}
