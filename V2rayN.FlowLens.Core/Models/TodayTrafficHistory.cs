namespace V2rayN.FlowLens.Core.Models;

public sealed record TodayTrafficHistory(
    DateOnly Date,
    IReadOnlyList<ApplicationTrafficSummary> Applications,
    IReadOnlyList<DomainTrafficSummary> Domains)
{
    public static TodayTrafficHistory Empty(DateOnly date)
    {
        return new TodayTrafficHistory(date, [], []);
    }
}
