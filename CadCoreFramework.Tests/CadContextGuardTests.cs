using System;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class CadContextGuardTests
    {
        // Reset guard state before each test — [ThreadStatic] persists across tests on the same thread.
        private void Reset() { if (CadContextGuard.IsActive) CadContextGuard.Exit(); }

        [Fact]
        public void IsActive_IsFalse_Initially()
        {
            Reset();
            CadContextGuard.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Enter_SetsIsActive_True()
        {
            Reset();
            CadContextGuard.Enter();
            try { CadContextGuard.IsActive.Should().BeTrue(); }
            finally { CadContextGuard.Exit(); }
        }

        [Fact]
        public void Exit_SetsIsActive_False()
        {
            Reset();
            CadContextGuard.Enter();
            CadContextGuard.Exit();
            CadContextGuard.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Enter_WhenAlreadyActive_ThrowsInvalidOperationException()
        {
            Reset();
            CadContextGuard.Enter();
            try
            {
                Action nestedEnter = () => CadContextGuard.Enter();
                nestedEnter.Should().Throw<InvalidOperationException>()
                    .WithMessage("*Nested CadContext.Execute*");
            }
            finally { CadContextGuard.Exit(); }
        }

        [Fact]
        public void Enter_AfterExit_Succeeds()
        {
            Reset();
            CadContextGuard.Enter();
            CadContextGuard.Exit();

            // Second Enter should not throw
            CadContextGuard.Enter();
            try { CadContextGuard.IsActive.Should().BeTrue(); }
            finally { CadContextGuard.Exit(); }
        }
    }
}
