namespace V2rayN.FlowLens.Core.Models;

public sealed record TrafficHistoryBrowserResult(
    bool IsSuccess,
    TodayTrafficHistory History,
    TodayHistoryState State);
