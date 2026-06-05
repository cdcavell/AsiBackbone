using System.Collections;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests;

internal static class ReadOnlyMetadataAssert
{
    public static void CannotMutateThroughCasts(IReadOnlyDictionary<string, string> metadata)
    {
        bool genericCastWasAvailable = AssertCannotMutateThroughGenericDictionary(metadata);
        bool nonGenericCastWasAvailable = AssertCannotMutateThroughNonGenericDictionary(metadata);

        Assert.True(
            genericCastWasAvailable || nonGenericCastWasAvailable ||
            (metadata is not IDictionary<string, string> && metadata is not IDictionary));
    }

    private static bool AssertCannotMutateThroughGenericDictionary(
        IReadOnlyDictionary<string, string> metadata)
    {
        const string setKey = "__mutation_set__";
        const string addKey = "__mutation_add__";

        if (metadata is not IDictionary<string, string> dictionary)
        {
            Assert.False(metadata is IDictionary<string, string>);
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
            Assert.False(metadata is IDictionary);
            return false;
        }

        _ = Assert.Throws<NotSupportedException>(() => dictionary[setKey] = "blocked");
        _ = Assert.Throws<NotSupportedException>(() => dictionary.Add(addKey, "blocked"));

        Assert.False(metadata.ContainsKey(setKey));
        Assert.False(metadata.ContainsKey(addKey));

        return true;
    }
}
