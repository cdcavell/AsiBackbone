namespace CDCavell.ASIBackbone.Core.Results;

/// <summary>
/// Represents the outcome of an ASI Backbone operation.
/// </summary>
public sealed class BackboneResult
{
    private const string DefaultFailureMessage = "Operation failed.";

    private static readonly IReadOnlyList<string> EmptyMessages =
        Array.AsReadOnly(Array.Empty<string>());

    private BackboneResult(bool succeeded, IReadOnlyList<string> messages)
    {
        Succeeded = succeeded;
        Messages = messages;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool Failed => !Succeeded;

    /// <summary>
    /// Gets the messages associated with the operation result.
    /// </summary>
    public IReadOnlyList<string> Messages { get; }

    /// <summary>
    /// Creates a successful result with no messages.
    /// </summary>
    /// <returns>A successful operation result.</returns>
    public static BackboneResult Success()
    {
        return new BackboneResult(true, EmptyMessages);
    }

    /// <summary>
    /// Creates a successful result with one message.
    /// </summary>
    /// <param name="message">The message associated with the successful result.</param>
    /// <returns>A successful operation result.</returns>
    public static BackboneResult Success(string message)
    {
        return new BackboneResult(true, NormalizeMessages([message]));
    }

    /// <summary>
    /// Creates a successful result with one or more messages.
    /// </summary>
    /// <param name="messages">The messages associated with the successful result.</param>
    /// <returns>A successful operation result.</returns>
    public static BackboneResult Success(IEnumerable<string> messages)
    {
        return new BackboneResult(true, NormalizeMessages(messages));
    }

    /// <summary>
    /// Creates a failed result with one message.
    /// </summary>
    /// <param name="message">The message associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static BackboneResult Failure(string message)
    {
        return new BackboneResult(false, NormalizeMessages([message], DefaultFailureMessage));
    }

    /// <summary>
    /// Creates a failed result with one or more messages.
    /// </summary>
    /// <param name="messages">The messages associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static BackboneResult Failure(IEnumerable<string> messages)
    {
        return new BackboneResult(false, NormalizeMessages(messages, DefaultFailureMessage));
    }

    private static IReadOnlyList<string> NormalizeMessages(
        IEnumerable<string>? messages,
        string? fallbackMessage = null)
    {
        string[] normalizedMessages = messages?
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message.Trim())
            .ToArray() ?? [];

        return normalizedMessages.Length == 0
            ? fallbackMessage is null
                ? EmptyMessages
                : Array.AsReadOnly([fallbackMessage])
            : Array.AsReadOnly(normalizedMessages);
    }
}
