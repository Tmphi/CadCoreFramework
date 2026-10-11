using System;
using CadCoreFramework.Core;
using CadCoreFramework.Utils;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class GuardTests
    {
        [Fact]
        public void NotNull_WithNullValue_ShouldThrow()
        {
            Action act = () => Guard.NotNull<object>(null, "myParam");
            act.Should().Throw<ArgumentNullException>().WithParameterName("myParam");
        }

        [Fact]
        public void NotNull_WithValidValue_ShouldReturnValue()
        {
            var obj = new object();
            var result = Guard.NotNull(obj, "myParam");
            result.Should().Be(obj);
        }

        [Fact]
        public void NotEmpty_WithEmptyString_ShouldThrow()
        {
            Action act = () => Guard.NotEmpty("  ", "myParam");
            act.Should().Throw<ArgumentException>().WithParameterName("myParam");
        }

        [Fact]
        public void IsTrue_WithFalse_ShouldThrow()
        {
            Action act = () => Guard.IsTrue(false, "myParam", "Message");
            act.Should().Throw<ArgumentException>().WithParameterName("myParam").WithMessage("Message*");
        }
    }
}
