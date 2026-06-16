namespace V2rayN.FlowLens.Core.Models;

public sealed record TodayHistoryState(
    DateOnly Date,
    string FilePath,
    string Status)
{
    public string DateDisplay => Date.ToString("yyyy-MM-dd");
}
