using Microsoft.CodeAnalysis;

namespace CadCoreFramework.Analyzers;

/// <summary>
/// Phân loại một kiểu vào tầng kiến trúc mà các rule CAD001/CAD002 áp dụng.
/// Theo tên (namespace chứa ".Services"/".ViewModels", hoặc tên lớp kết thúc bằng "Service"/"ViewModel")
/// để vẫn đúng sau khi template đổi namespace gốc. Xét cả lớp bao ngoài (nested type).
/// </summary>
internal static class ArchitectureLayers
{
    /// <summary>"Services", "ViewModels", hoặc null nếu kiểu không thuộc hai tầng đó.</summary>
    public static string? TierOf(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? t = type; t != null; t = t.ContainingType)
        {
            string name = t.Name;
            // Hạ tầng: CadContext là nơi DUY NHẤT đúng để dùng MdiActiveDocument.
            if (name is "CadContext" or "AppBootstrapper") return null;

            string ns = t.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (ns.Contains(".Services") || name.EndsWith("Service")) return "Services";
            if (ns.Contains(".ViewModels") || name.EndsWith("ViewModel")) return "ViewModels";
        }
        return null;
    }
}
