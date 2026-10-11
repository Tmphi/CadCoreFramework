using System;
using System.Threading.Tasks;
using CadCoreFramework.Core;
using CadCoreFramework.Testing.Fakes;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Minh họa cách dùng FakeCadContext để kiểm thử Service mà không cần cài AutoCAD.
    ///
    /// NGUYÊN TẮC:
    /// - Set ExecuteAction = false khi bạn chỉ muốn kiểm tra control flow (Execute được gọi mấy lần,
    ///   undoMarkName đúng không) mà không chạy lambda.
    /// - Để kiểm tra logic bên trong lambda (không dùng drawing extensions), để ExecuteAction = true.
    /// - ExceptionToThrow để test nhánh xử lý lỗi trong Service.
    /// </summary>
    public class FakeCadContextTests
    {
        // ─── Minimal stub service để demo pattern ────────────────────────────────

        private class PanelDrawService
        {
            private readonly ICadContext _ctx;
            public PanelDrawService(ICadContext ctx) => _ctx = ctx;

            public void DrawPanel(string panelName)
                => _ctx.Execute(_ => { }, undoMarkName: $"Vẽ {panelName}");

            public async Task DrawPanelAsync(string panelName)
                => await _ctx.ExecuteAsync(() =>
                {
                    Action<ICadScope> draw = _ => { };
                    return Task.FromResult(draw);
                }, undoMarkName: $"Vẽ {panelName} (async)");

            public CadResult<int> CountPanels()
                => _ctx.Execute(_ => 42, undoMarkName: "Count");

            public void DrawBatch(string[] names)
                => _ctx.ExecuteBatch(names, (_, n) => { }, undoMarkName: "Batch Panels");
        }

        // ─── Control-flow tests (ExecuteAction = false) ──────────────────────────

        [Fact]
        public void Execute_CalledOnce_WhenServiceDrawsPanel()
        {
            var ctx = new FakeCadContext { ExecuteAction = false };
            new PanelDrawService(ctx).DrawPanel("MSB-01");
            ctx.ExecuteCount.Should().Be(1);
        }

        [Fact]
        public void Execute_UndoMarkName_MatchesServiceValue()
        {
            var ctx = new FakeCadContext { ExecuteAction = false };
            new PanelDrawService(ctx).DrawPanel("MSB-01");
            ctx.LastUndoMarkName.Should().Be("Vẽ MSB-01");
        }

        [Fact]
        public async Task ExecuteAsync_CalledOnce_ForAsyncService()
        {
            var ctx = new FakeCadContext { ExecuteAction = false };
            await new PanelDrawService(ctx).DrawPanelAsync("MSB-02");
            ctx.ExecuteAsyncCount.Should().Be(1);
            ctx.LastUndoMarkName.Should().Be("Vẽ MSB-02 (async)");
        }

        [Fact]
        public void ExecuteBatch_CalledOnce_CountIsCorrect()
        {
            var ctx = new FakeCadContext { ExecuteAction = false };
            new PanelDrawService(ctx).DrawBatch(new[] { "A", "B", "C" });
            ctx.ExecuteBatchCount.Should().Be(1);
            ctx.LastUndoMarkName.Should().Be("Batch Panels");
        }

        // ─── Return-value tests (ExecuteAction = true, lambda không dùng database) ─

        [Fact]
        public void Execute_Generic_ReturnsValueFromLambda()
        {
            var ctx = new FakeCadContext();
            var result = new PanelDrawService(ctx).CountPanels();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(42);
        }

        // ─── Error-injection tests ───────────────────────────────────────────────

        [Fact]
        public void Execute_WhenExceptionInjected_ThrowsExpectedException()
        {
            var ctx = new FakeCadContext
            {
                ExceptionToThrow = new InvalidOperationException("Database not open")
            };
            Action act = () => new PanelDrawService(ctx).DrawPanel("MSB-01");
            act.Should().Throw<InvalidOperationException>().WithMessage("Database not open");
        }

        [Fact]
        public void Execute_Generic_WhenExceptionInjected_ReturnsFailure()
        {
            var ctx = new FakeCadContext
            {
                ExceptionToThrow = new InvalidOperationException("oops")
            };
            var result = new PanelDrawService(ctx).CountPanels();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("oops");
        }
    }
}
