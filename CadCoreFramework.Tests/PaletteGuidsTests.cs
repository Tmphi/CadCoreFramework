using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// PaletteGuids — Guid ổn định cho PaletteSet để AutoCAD nhớ vị trí (B-04). Thuần .NET.
    /// </summary>
    public class PaletteGuidsTests
    {
        private static readonly Guid PluginA = new Guid("11111111-2222-3333-4444-555555555555");
        private static readonly Guid PluginB = new Guid("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

        [Fact]
        public void For_SameInputs_ReturnsSameGuid()
        {
            PaletteGuids.For(PluginA, "Grid Marker").Should().Be(PaletteGuids.For(PluginA, "Grid Marker"));
        }

        [Fact]
        public void For_IgnoresCaseAndSurroundingWhitespace()
        {
            // PaletteService tra palette theo tên không phân biệt hoa/thường → Guid cũng vậy.
            PaletteGuids.For(PluginA, " grid marker ").Should().Be(PaletteGuids.For(PluginA, "GRID MARKER"));
        }

        [Fact]
        public void For_DifferentNames_ReturnDifferentGuids()
        {
            PaletteGuids.For(PluginA, "Grid Marker").Should().NotBe(PaletteGuids.For(PluginA, "Layer Manager"));
        }

        [Fact]
        public void For_SameNameInDifferentPlugins_ReturnDifferentGuids()
        {
            // Hai plugin dựng từ template này, cùng tên palette, không được dùng chung vị trí đã lưu.
            PaletteGuids.For(PluginA, "Debug Inspector").Should().NotBe(PaletteGuids.For(PluginB, "Debug Inspector"));
        }

        [Fact]
        public void For_ProducesVersion8RfcVariantUuid()
        {
            string s = PaletteGuids.For(PluginA, "Grid Marker").ToString("D");

            s[14].Should().Be('8', "nibble phiên bản của UUID dựa trên tên kiểu SHA-256 (RFC 9562 v8)");
            "89ab".Should().Contain(s[19].ToString(), "variant RFC = 10xx");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void For_BlankName_Throws(string? name)
        {
            Action act = () => PaletteGuids.For(PluginA, name!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void PluginIdOf_PluginAssembly_UsesAssemblyGuidAttribute()
        {
            Assembly plugin = typeof(PaletteService).Assembly;
            var expected = Guid.Parse(plugin.GetCustomAttribute<GuidAttribute>()!.Value);

            PaletteGuids.PluginIdOf(plugin).Should().Be(expected);
        }

        [Fact]
        public void PluginIdOf_AssemblyWithoutGuidAttribute_IsDerivedFromNameAndStable()
        {
            Assembly a1 = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NoGuidPlugin"), AssemblyBuilderAccess.Run);
            Assembly a2 = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NoGuidPlugin"), AssemblyBuilderAccess.Run);

            Guid id = PaletteGuids.PluginIdOf(a1);
            id.Should().NotBe(Guid.Empty);
            PaletteGuids.PluginIdOf(a2).Should().Be(id);
        }
    }
}
