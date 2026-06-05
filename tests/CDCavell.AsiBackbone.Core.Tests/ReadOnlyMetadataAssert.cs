using System.Collections;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests;

internal static class ReadOnlyMetadataAssert
{
    public static void CannotMutateThroughCasts(IReadOnlyDictionary<string, string> metadata)
    {
        AssertCannotMutateThroughGenericDictionary(metadata);
        AssertCannotMutateThroughNonGenericDictionary(metadata);
    }

    private static void AssertCannotMutateThroughGenericDictionary(
        IReadOnlyDictionary<string, string> metadata)
    {
        const string setKey = "__mutation_set__";
        const string addKey = "__mutation_add__";

        if (metadata is not IDictionary<string, string> dictionary)
        {
            return;
        }

        _ = Assert.Throws<NotSupportedException>(() => dictionary[setKey] = "blocked");
        _ = Assert.Throws<NotSupportedException>(() => dictionary.Add(addKey, "blocked"));

        Assert.False(metadata.ContainsKey(setKey));
        Assert.False(metadata.ContainsKey(addKey));
    }

    private static void AssertCannotMutateThroughNonGenericDictionary(
        IReadOnlyDictionary<string, string> metadata)
    {
        const string setKey = "__non_generic_mutation_set__";
        const string addKey = "__non_generic_mutation_add__";

        if (metadata is not IDictionary dictionary)
        {
            return;
        }

        _ = Assert.Throws<NotSupportedException>(() => dictionary[setKey] = "blocked");
        _ = Assert.Throws<NotSupportedException>(() => dictionary.Add(addKey, "blocked"));

        Assert.False(metadata.ContainsKey(setKey));
        Assert.False(metadata.ContainsKey(addKey));
    }
}
