using System.Windows;
using V2rayN.FlowLens.Core;

namespace V2rayN.FlowLens.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ApplyLanguageResources(new SettingsStore().Load().UiLanguage);
        base.OnStartup(e);
    }

    private static void ApplyLanguageResources(string uiLanguage)
    {
        var source = string.Equals(uiLanguage, "简体中文", StringComparison.OrdinalIgnoreCase)
            ? "Resources/Strings.zh-CN.xaml"
            : "Resources/Strings.en-US.xaml";

        var dictionaries = Current.Resources.MergedDictionaries;
        for (var index = dictionaries.Count - 1; index >= 0; index--)
        {
            var dictionary = dictionaries[index];
            if (dictionary.Source?.OriginalString.StartsWith("Resources/Strings.", StringComparison.OrdinalIgnoreCase) == true)
            {
                dictionaries.RemoveAt(index);
            }
        }

        dictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(source, UriKind.Relative)
        });
    }
}
