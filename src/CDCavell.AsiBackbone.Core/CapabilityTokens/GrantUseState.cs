namespace CDCavell.AsiBackbone.Core.CapabilityTokens;

public enum GrantUseState
{
    Accepted = 0,
    UseLimitExceeded = 1,
    Stopped = 2,
    Cancelled = 3,
    Unavailable = 4
}
