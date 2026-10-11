using System;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class CadResultTests
    {
        [Fact]
        public void Success_ShouldCreateSuccessfulResult()
        {
            var result = CadResult<string>.Success("OK");
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be("OK");
            result.ErrorMessage.Should().BeNull();
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void Fail_ShouldCreateFailedResult()
        {
            var ex = new Exception("Test error");
            var result = CadResult<string>.Fail("Failed", ex);
            
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().Be("Failed");
            result.Exception.Should().Be(ex);
        }

        [Fact]
        public void VoidResult_Ok_ShouldBeSuccess()
        {
            var result = CadResult.Ok();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void VoidResult_Fail_ShouldBeFailure()
        {
            var result = CadResult.Fail("Error");
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Error");
        }
    }
}
