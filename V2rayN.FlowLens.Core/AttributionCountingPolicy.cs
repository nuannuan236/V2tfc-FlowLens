using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public static class AttributionCountingPolicy
{
    public static bool CountsAsApplicationTraffic(AttributedConnection connection)
    {
        if (connection.ProcessId is null or <= 0)
        {
            return false;
        }

        if (connection.Application.Equals("Idle", StringComparison.OrdinalIgnoreCase) ||
            connection.Application.Equals("Idle.exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !connection.Status.Equals("LogOnly", StringComparison.OrdinalIgnoreCase) &&
            !connection.Status.Equals("Ambiguous", StringComparison.OrdinalIgnoreCase) &&
            !connection.Status.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
    }
}
