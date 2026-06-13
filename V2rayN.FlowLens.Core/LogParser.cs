using System.Globalization;
using System.Text.RegularExpressions;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed partial class LogParser
{
    public bool TryParse(string line, out LogConnectionRecord? record)
    {
        record = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var match = XrayAcceptedLineRegex().Match(line);
        if (!match.Success)
        {
            return false;
        }

        var timestamp = TryParseTimestamp(line, out var parsedTimestamp)
            ? parsedTimestamp
            : DateTime.MinValue;

        if (!TryParseEndpoint(match.Groups["source"].Value, out var sourceAddress, out var sourcePort))
        {
            return false;
        }

        var target = ParseTarget(match.Groups["target"].Value);
        if (target.Host.Length == 0)
        {
            return false;
        }

        record = new LogConnectionRecord(
            timestamp,
            sourceAddress,
            sourcePort,
            target.Host,
            target.Port,
            match.Groups["inbound"].Value.Trim(),
            match.Groups["outbound"].Value.Trim(),
            line);

        return true;
    }

    private static bool TryParseTimestamp(string line, out DateTime timestamp)
    {
        timestamp = default;

        var match = TimestampRegex().Match(line);
        return match.Success
            && DateTime.TryParseExact(
                match.Value,
                "yyyy/MM/dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out timestamp);
    }

    private static bool TryParseEndpoint(string value, out string address, out int port)
    {
        address = string.Empty;
        port = 0;

        var separatorIndex = value.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            return false;
        }

        if (!int.TryParse(value[(separatorIndex + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out port))
        {
            return false;
        }

        address = StripKnownScheme(value[..separatorIndex]).Trim('[', ']');
        return address.Length > 0;
    }

    private static (string Host, int? Port) ParseTarget(string value)
    {
        var normalized = value.Trim();
        if (normalized.StartsWith("//", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = StripKnownScheme(normalized);

        var separatorIndex = normalized.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == normalized.Length - 1)
        {
            return (normalized.Trim('[', ']'), null);
        }

        if (!int.TryParse(normalized[(separatorIndex + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var port))
        {
            return (normalized.Trim('[', ']'), null);
        }

        return (normalized[..separatorIndex].Trim('[', ']'), port);
    }

    private static string StripKnownScheme(string value)
    {
        var separatorIndex = value.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return value;
        }

        var scheme = value[..separatorIndex];
        return scheme.Equals("tcp", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("udp", StringComparison.OrdinalIgnoreCase)
                ? value[(separatorIndex + 1)..]
                : value;
    }

    [GeneratedRegex(@"\d{4}/\d{2}/\d{2}\s+\d{2}:\d{2}:\d{2}", RegexOptions.Compiled)]
    private static partial Regex TimestampRegex();

    [GeneratedRegex(@"\bfrom\s+(?<source>\S+)\s+accepted\s+(?<target>\S+).*?\[(?<inbound>[^\]\r\n]+?)\s*(?:->|>>)\s*(?<outbound>[^\]\r\n]+?)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex XrayAcceptedLineRegex();
}
