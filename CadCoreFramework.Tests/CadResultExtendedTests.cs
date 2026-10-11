using System;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Tests cho CadResult — đối tượng kết quả thống nhất của framework.
    /// Bao gồm cả CadResult (void) và CadResult&lt;T&gt; (generic).
    /// </summary>
    public class CadResultExtendedTests
    {
        [Fact]
        public void GenericSuccess_WithNullValue_ShouldBeSuccessWithNull()
        {
            var result = CadResult<string>.Success(null);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
        }

        [Fact]
        public void GenericSuccess_WithComplexType_ShouldRetainValue()
        {
            var data = new { Name = "Test", Count = 42 };
            var result = CadResult<object>.Success(data);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(data);
        }

        [Fact]
        public void GenericFail_WithoutException_ShouldHaveNullException()
        {
            var result = CadResult<int>.Fail("Error message");
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Error message");
            result.Exception.Should().BeNull();
            result.Value.Should().Be(default(int));
        }

        [Fact]
        public void GenericFail_WithException_ShouldRetainExceptionDetails()
        {
            var inner = new InvalidOperationException("Inner error");
            var ex = new Exception("Outer error", inner);
            var result = CadResult<string>.Fail("Wrapped", ex);

            result.Exception.Should().Be(ex);
            result.Exception.InnerException.Should().Be(inner);
        }

        [Fact]
        public void VoidResult_Ok_ShouldHaveNullValue()
        {
            var result = CadResult.Ok();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void VoidResult_Fail_ShouldPreserveException()
        {
            var ex = new ArgumentException("bad arg");
            var result = CadResult.Fail("failed", ex);
            result.IsSuccess.Should().BeFalse();
            result.Exception.Should().Be(ex);
        }
    }
}
