using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Finanzuebersicht.Tests.Views;

public class SettingsPageResourceTests
{
    [Fact]
    public void SettingsPage_StaticResourcesResolveBeforeNavigation()
    {
        var root = FindRepositoryRoot();
        var page = XDocument.Load(Path.Combine(root, "Finanzuebersicht/Views/SettingsPage.xaml"));
        var keys = new HashSet<string>(StringComparer.Ordinal);
        CollectKeys(Path.Combine(root, "Finanzuebersicht/App.xaml"), keys);
        CollectKeys(Path.Combine(root, "Finanzuebersicht/Views/SettingsPage.xaml"), keys);

        // A missing StaticResource throws while MAUI constructs the page, before OnAppearing.
        var missing = page.Descendants().Attributes()
            .SelectMany(attribute => Regex.Matches(attribute.Value, @"\{StaticResource\s+([^\s},]+)")
                .Select(match => match.Groups[1].Value))
            .Where(key => !keys.Contains(key))
            .Distinct()
            .ToArray();

        Assert.True(missing.Length == 0, "Unresolved Settings resources: " + string.Join(", ", missing));
    }

    private static void CollectKeys(string path, HashSet<string> keys)
    {
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2009/xaml";
        var document = XDocument.Load(path);
        foreach (var element in document.Descendants())
        {
            if (element.Attribute(xaml + "Key") is { } key)
                keys.Add(key.Value);
            if (element.Name.LocalName == "ResourceDictionary" && element.Attribute("Source") is { } source)
                CollectKeys(Path.Combine(Path.GetDirectoryName(path)!, source.Value), keys);
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Finanzuebersicht/Views/SettingsPage.xaml")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Cannot locate SettingsPage.xaml.");
    }
}
