using System.Windows;

namespace V2rayN.FlowLens.App;

internal static class UiText
{
    public static string Get(string key, string fallback)
    {
        return System.Windows.Application.Current.TryFindResource(key) is string value && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}
