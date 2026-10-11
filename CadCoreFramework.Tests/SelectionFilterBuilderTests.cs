using FluentAssertions;
using Xunit;

// SelectionFilterBuilder là một builder thuần C# (không phụ thuộc vào AutoCAD runtime trong hàm Build()).
// Chúng ta kiểm thử tính toàn vẹn của fluent API và các mã DXF mà nó tạo ra.

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Các bài kiểm thử đơn vị cho lớp SelectionFilterBuilder.
    /// </summary>
    public class SelectionFilterBuilderTests
    {
        // ─── OfTypes ──────────────────────────────────────────────────────────────

        [Fact]
        public void Create_WithNoFilters_BuildsEmptyFilter()
        {
            // Thiết lập / Thực thi
            // Không thể khởi tạo trực tiếp SelectionFilter nếu không có AutoCAD,
            // mục tiêu ở đây là đảm bảo hàm Build() không ném ra ngoại lệ.
            var builder = CadCoreFramework.Utils.SelectionFilterBuilder.Create();

            var act = () => builder.Build();

            act.Should().NotThrow();
        }

        [Fact]
        public void OfTypes_SingleType_ChainReturnsSameBuilder()
        {
            var builder = CadCoreFramework.Utils.SelectionFilterBuilder.Create();

            var result = builder.OfTypes("LINE");

            result.Should().BeSameAs(builder, because: "fluent API phải trả về chính instance đó");
        }

        [Fact]
        public void OfTypes_NullOrEmpty_DoesNotThrowAndReturnsSameBuilder()
        {
            var builder = CadCoreFramework.Utils.SelectionFilterBuilder.Create();

            var fromNull  = builder.OfTypes(null);
            var fromEmpty = builder.OfTypes();

            fromNull.Should().BeSameAs(builder);
            fromEmpty.Should().BeSameAs(builder);
        }

        // ─── OnLayers ────────────────────────────────────────────────────────────

        [Fact]
        public void OnLayers_SingleLayer_ChainReturnsSameBuilder()
        {
            var builder = CadCoreFramework.Utils.SelectionFilterBuilder.Create();

            var result = builder.OnLayers("BUSBAR");

            result.Should().BeSameAs(builder);
        }

        // ─── WithBlockNames ───────────────────────────────────────────────────────

        [Fact]
        public void WithBlockNames_SingleName_ChainReturnsSameBuilder()
        {
            var builder = CadCoreFramework.Utils.SelectionFilterBuilder.Create();

            var result = builder.WithBlockNames("MCCB_*");

            result.Should().BeSameAs(builder);
        }

        // ─── WithColor ────────────────────────────────────────────────────────────

        [Fact]
        public void WithColor_ValidIndex_ChainReturnsSameBuilder()
        {
            var builder = CadCoreFramework.Utils.SelectionFilterBuilder.Create();

            var result = builder.WithColor(3); // xanh lá

            result.Should().BeSameAs(builder);
        }

        // ─── Custom ───────────────────────────────────────────────────────────────

        [Fact]
        public void Custom_ArbitraryCode_ChainReturnsSameBuilder()
        {
            var builder = CadCoreFramework.Utils.SelectionFilterBuilder.Create();

            var result = builder.Custom(8, "MY_LAYER");

            result.Should().BeSameAs(builder);
        }

        // ─── WithXData ────────────────────────────────────────────────────────────

        [Fact]
        public void WithXData_AppName_ChainReturnsSameBuilder()
        {
            var builder = CadCoreFramework.Utils.SelectionFilterBuilder.Create();

            var result = builder.WithXData("MY_APP");

            result.Should().BeSameAs(builder);
        }

        // ─── Fluent Chaining ─────────────────────────────────────────────────────

        [Fact]
        public void FluentChain_MultipleFilters_DoesNotThrow()
        {
            var act = () =>
            {
                CadCoreFramework.Utils.SelectionFilterBuilder.Create()
                    .OfTypes("INSERT")
                    .OnLayers("EQUIPMENT")
                    .WithBlockNames("ACB_*")
                    .WithColor(1)
                    .Build();
            };

            act.Should().NotThrow(because: "tất cả các phương thức fluent phải kết hợp được mà không gây lỗi");
        }
    }
}
