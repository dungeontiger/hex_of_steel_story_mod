using System;

namespace Story.Core;

public static class ObjectiveChanges
{
    public static string? Describe(string name, string coordinates, string previousOwner, string owner)
        => string.Equals(previousOwner, owner, StringComparison.Ordinal) ? null
            : $"{name} at {coordinates} was captured or transferred to {owner} and lost by {previousOwner}.";
}
