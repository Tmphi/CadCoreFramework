using System;
using CadCoreFramework.Utils;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// SymbolNameRules — luật tên ký hiệu dùng chung cho Ensure*/CreateBlockDefinition/EnsureRegApp
    /// và GridSpec (B-16). AutoCAD 2024 ném eInvalidInput (không nói lý do) với tên sai.
    /// </summary>
    public class SymbolNameRulesTests
    {
        [Theory]
        [InlineData("0")]
        [InlineData("E-POWER")]
        [InlineData("Lưới trục A")]       // Unicode, khoảng trắng: hợp lệ
        [InlineData("LAB#1")]             // '#', '.', '@', '~', '[', ']' hợp lệ trong TÊN (chỉ là wildcard trong filter)
        [InlineData("M.EQUIP@2[x]~")]
        public void IsValid_ValidNames_ReturnsTrue(string name)
        {
            SymbolNameRules.IsValid(name).Should().BeTrue();
            SymbolNameRules.GetError(name).Should().BeNull();
        }

        [Theory]
        [InlineData('<')] [InlineData('>')] [InlineData('/')] [InlineData('\\')] [InlineData('"')]
        [InlineData(':')] [InlineData(';')] [InlineData('?')] [InlineData('*')] [InlineData('|')]
        [InlineData(',')] [InlineData('=')] [InlineData('`')]
        public void GetError_InvalidCharacter_NamesTheCharacter(char c)
        {
            string name = "A" + c + "B";

            SymbolNameRules.ContainsInvalidCharacter(name).Should().BeTrue();
            SymbolNameRules.GetError(name).Should().Contain($"'{c}'");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetError_Blank_ReportsEmpty(string? name)
        {
            SymbolNameRules.IsValid(name).Should().BeFalse();
            SymbolNameRules.ContainsInvalidCharacter(name).Should().BeFalse("trống không phải là 'ký tự cấm'");
        }

        [Fact]
        public void GetError_TooLong_ReportsLength()
        {
            string ok = new string('A', SymbolNameRules.MaxLength);
            string tooLong = ok + "A";

            SymbolNameRules.IsValid(ok).Should().BeTrue();
            SymbolNameRules.GetError(tooLong).Should().Contain(SymbolNameRules.MaxLength.ToString());
        }

        [Fact]
        public void Validate_Invalid_ThrowsArgumentExceptionWithParamName()
        {
            Action act = () => SymbolNameRules.Validate("A*B", "layerName");

            act.Should().Throw<ArgumentException>()
                .Where(e => e.ParamName == "layerName" && e.Message.Contains("'*'"));
        }

        [Fact]
        public void Validate_Valid_ReturnsName()
        {
            SymbolNameRules.Validate("E-POWER", "layerName").Should().Be("E-POWER");
        }
    }
}
