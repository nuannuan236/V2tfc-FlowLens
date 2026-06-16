namespace V2rayN.FlowLens.Core.Models;

public sealed record TrafficHistoryDateOption(DateOnly Date, string FilePath)
{
    public string Display => Date.ToString("yyyy-MM-dd");
}
