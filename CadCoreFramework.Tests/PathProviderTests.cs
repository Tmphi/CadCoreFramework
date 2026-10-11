using System;
using System.IO;
using CadCoreFramework.Services;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Pure-C# tests cho PathProvider — không cần AutoCAD runtime.
    /// </summary>
    public class PathProviderTests
    {
        [Fact]
        public void GetPluginDirectory_ReturnsExistingDirectory()
        {
            var provider = new PathProvider();
            string dir = provider.GetPluginDirectory();

            dir.Should().NotBeNullOrEmpty();
            Directory.Exists(dir).Should().BeTrue();
        }

        [Fact]
        public void GetAppDataDirectory_IsUnderAppData_AndEndsWithAppName()
        {
            var provider = new PathProvider();
            string dir = provider.GetAppDataDirectory();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            dir.Should().StartWith(appData);
            Path.GetFileName(dir).Should().Be("CadCoreFramework");
        }

        [Fact]
        public void GetTemplatesDirectory_IsPluginDirectoryPlusTemplates()
        {
            var provider = new PathProvider();
            string templates = provider.GetTemplatesDirectory();
            string plugin = provider.GetPluginDirectory();

            templates.Should().Be(Path.Combine(plugin, "Templates"));
        }
    }
}
