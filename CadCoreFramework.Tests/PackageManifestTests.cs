using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Deployment/PackageContents.xml phải liệt kê ĐÚNG các lệnh của plugin (B-27): thiếu lệnh → khi
    /// chuyển sang nạp theo lệnh (demand-load), gõ lệnh đó không nạp được plugin; thừa lệnh → manifest nói dối.
    /// Lệnh DEMO_* (thư mục Demos/, tùy chọn IncludeDemos của template) cố ý không liệt kê.
    /// </summary>
    public class PackageManifestTests
    {
        private static readonly string[] SourceFolders = { "Commands", "Samples", "Core", "Services", "Utils", "ViewModels", "Views", "Extensions" };

        [Fact]
        public void EveryComponentEntry_ListsExactlyTheNonDemoCommands()
        {
            string root = RepoRoot();
            var commands = SourceFolders
                .Select(f => Path.Combine(root, f))
                .Where(Directory.Exists)
                .SelectMany(d => Directory.GetFiles(d, "*.cs", SearchOption.AllDirectories))
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\[CommandMethod\(\s*""([^""]+)""").Cast<Match>())
                .Select(m => m.Groups[1].Value.ToUpperInvariant())
                .Distinct()
                .ToList();
            commands.Should().Contain(new[] { "CORE_INFO", "SAMPLE_GRID" }, "quét nguồn phải tìm thấy lệnh");

            XDocument manifest = XDocument.Load(Path.Combine(root, "Deployment", "PackageContents.xml"));
            var entries = manifest.Descendants("ComponentEntry").ToList();
            entries.Should().NotBeEmpty();

            foreach (XElement entry in entries)
            {
                var listed = entry.Descendants("Command").Select(c => ((string)c.Attribute("Global")!).ToUpperInvariant()).ToList();
                listed.Should().OnlyHaveUniqueItems();
                listed.Should().BeEquivalentTo(commands, $"ComponentEntry {(string?)entry.Attribute("ModuleName")}");
            }
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CadCoreFramework.csproj")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Không tìm thấy thư mục gốc repo (CadCoreFramework.csproj).");
        }
    }
}
