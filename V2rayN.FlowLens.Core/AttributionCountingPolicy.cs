using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public static class AttributionCountingPolicy
{
    public static bool CountsAsApplicationTraffic(AttributedConnection connection)
    {
        if (connection.ProcessId is null)
        {
            return false;
        }

        return !connection.Status.Equals("LogOnly", StringComparison.OrdinalIgnoreCase) &&
            !connection.Status.Equals("Ambiguous", StringComparison.OrdinalIgnoreCase) &&
            !connection.Status.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
    }
}
