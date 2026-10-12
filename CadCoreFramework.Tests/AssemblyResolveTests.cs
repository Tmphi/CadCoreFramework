using System;
using System.Reflection;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// AppBootstrapper.FindLoadedAssembly — chống nạp bản thứ hai của plugin khi plugin được nạp
    /// từ byte array (lỗi WPF "does not have a resource identified by the URI").
    /// </summary>
    public class AssemblyResolveTests
    {
        private static readonly Assembly Plugin = typeof(AppBootstrapper).Assembly;
        private static readonly string PluginName = Plugin.GetName().Name!;
        private static readonly Assembly[] Loaded = { typeof(object).Assembly, typeof(FactAttribute).Assembly, Plugin };

        [Fact]
        public void SameNameAndVersion_ReturnsAlreadyLoadedAssembly()
        {
            var requested = new AssemblyName(Plugin.FullName!);

            AppBootstrapper.FindLoadedAssembly(requested, Loaded).Should().BeSameAs(Plugin);
        }

        [Fact]
        public void NameOnly_MatchesIgnoringCase()
        {
            var requested = new AssemblyName(PluginName.ToUpperInvariant());

            AppBootstrapper.FindLoadedAssembly(requested, Loaded).Should().BeSameAs(Plugin);
        }

        [Fact]
        public void DifferentVersion_IsNotReturned()
        {
            var requested = new AssemblyName(PluginName) { Version = new Version(99, 0, 0, 0) };

            AppBootstrapper.FindLoadedAssembly(requested, Loaded).Should().BeNull();
        }

        [Fact]
        public void NotLoaded_ReturnsNull()
        {
            AppBootstrapper.FindLoadedAssembly(new AssemblyName("Some.Missing.Assembly"), Loaded).Should().BeNull();
        }

        [Fact]
        public void NullArguments_ReturnNull()
        {
            AppBootstrapper.FindLoadedAssembly(null!, Loaded).Should().BeNull();
            AppBootstrapper.FindLoadedAssembly(new AssemblyName("x"), null!).Should().BeNull();
        }
    }
}
