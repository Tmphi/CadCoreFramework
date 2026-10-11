using System;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Utils;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Tests cho Guard — mở rộng bao phủ NotEmpty return value, ObjectId.Null, IsTrue success.
    /// </summary>
    public class GuardExtendedTests
    {
        [Fact]
        public void NotNull_WithValidReference_ShouldReturnSameReference()
        {
            var expected = "hello";
            var result = Guard.NotNull(expected, "param");
            result.Should().BeSameAs(expected);
        }

        [Fact]
        public void NotEmpty_WithValidString_ShouldReturnSameString()
        {
            var result = Guard.NotEmpty("valid text", "param");
            result.Should().Be("valid text");
        }

        [Fact]
        public void NotEmpty_WithNullString_ShouldThrow()
        {
            Action act = () => Guard.NotEmpty(null, "param");
            act.Should().Throw<ArgumentException>().WithParameterName("param");
        }

        // Removed NotNull_ObjectId test: AutoCAD API throws InvalidProgramException outside acad.exe

        [Fact]
        public void IsTrue_WithTrue_ShouldNotThrow()
        {
            Action act = () => Guard.IsTrue(true, "param", "Should not throw");
            act.Should().NotThrow();
        }

        [Fact]
        public void IsTrue_ErrorMessage_ShouldContainCustomMessage()
        {
            Action act = () => Guard.IsTrue(false, "param", "Custom error message");
            act.Should().Throw<ArgumentException>()
                .WithMessage("Custom error message*");
        }
    }
}
