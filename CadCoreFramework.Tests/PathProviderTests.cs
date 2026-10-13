using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
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

        [Fact]
        public void DirectoryOf_AssemblyLoadedFromBytes_ThrowsClearError()
        {
            // Đúng kịch bản tool nạp plugin từ byte array (chống khóa DLL): Location == "" (B-11).
            string path = typeof(FactAttribute).Assembly.Location;
            Assembly fromBytes = Assembly.Load(File.ReadAllBytes(path));
            fromBytes.Location.Should().BeEmpty();

            Action act = () => PathProvider.DirectoryOf(fromBytes);

            act.Should().Throw<InvalidOperationException>().WithMessage("*byte array*");
        }

        [Fact]
        public void DirectoryOf_DynamicAssembly_ThrowsClearError()
        {
            Assembly dynamic = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicForTest"), AssemblyBuilderAccess.Run);

            Action act = () => PathProvider.DirectoryOf(dynamic);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void DirectoryOf_FileAssembly_ReturnsItsDirectory()
        {
            Assembly asm = typeof(PathProvider).Assembly;

            PathProvider.DirectoryOf(asm).Should().Be(Path.GetDirectoryName(asm.Location));
        }
    }
}
