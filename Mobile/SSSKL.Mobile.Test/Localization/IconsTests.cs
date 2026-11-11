using System.Reflection;
using FluentAssertions;
using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Test.Localization;

/// <summary>The icon SVGs are third-party (Tabler Icons, MIT): every copy must carry the license.</summary>
[TestClass]
public class IconsTests
{
    private static readonly string ImagesDirectory = FindImagesDirectory();

    private static IEnumerable<string> IconFiles() =>
        Directory.EnumerateFiles(ImagesDirectory, "*.svg")
            .Where(f => Path.GetFileName(f).StartsWith("ti_", StringComparison.Ordinal) ||
                        Path.GetFileName(f).StartsWith("tab_", StringComparison.Ordinal));

    [TestMethod]
    public void EveryIconFile_CarriesTheTablerMitLicense()
    {
        var files = IconFiles().ToList();
        files.Should().NotBeEmpty();

        foreach (var file in files)
        {
            var svg = File.ReadAllText(file);
            svg.Should().Contain("Tabler Icons", Path.GetFileName(file))
                .And.Contain("MIT License")
                .And.Contain("Copyright (c) 2020-2026 Paweł Kuna")
                .And.Contain("The above copyright notice and this permission notice shall be included in all")
                .And.Contain("THE SOFTWARE IS PROVIDED \"AS IS\"");
        }
    }

    [TestMethod]
    public void EveryIcon_HasAnSvg()
    {
        var names = typeof(Icons).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string)f.GetValue(null)!)
            .ToList();
        names.Should().NotBeEmpty();

        foreach (var name in names)
        {
            // MAUI turns name.svg into name.png.
            var svg = Path.Combine(ImagesDirectory, Path.ChangeExtension(name, ".svg"));
            File.Exists(svg).Should().BeTrue($"{name} needs {Path.GetFileName(svg)}");
        }
    }

    private static string FindImagesDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "SSSKL.Mobile", "Resources", "Images");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new DirectoryNotFoundException("SSSKL.Mobile/Resources/Images not found above the test output");
    }
}
