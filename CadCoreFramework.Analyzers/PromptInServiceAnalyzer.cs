using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CadCoreFramework.Analyzers;

/// <summary>
/// CAD002 — Services và ViewModels không được hỏi người dùng qua dòng lệnh
/// (CadPromptHelper.* hoặc Editor.GetPoint/GetString/GetSelection/…).
///
/// LÝ DO:
/// CAD001 chặn MdiActiveDocument theo cú pháp, nhưng CadPromptHelper tự lấy Editor bên trong — Service
/// gọi nó vẫn "lọt" (docs/training/T09 B-18). Prompt trong Service:
///   • Service không còn gọi được từ Palette/batch/unit test (không có ai trả lời prompt).
///   • Trộn thu thập input (việc của Command) với nghiệp vụ.
///
/// Thay vì: Service gọi CadPromptHelper.GetPoint(...)
/// Dùng:    Command hỏi người dùng, rồi truyền kết quả (Point3d, GridSpec…) vào Service.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PromptInServiceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CAD002";

    /// <summary>Các phương thức của Editor HỎI người dùng (Get* khác như GetCurrentView thì không).</summary>
    private static readonly ImmutableHashSet<string> EditorPromptMethods = ImmutableHashSet.Create(
        "GetPoint", "GetCorner", "GetDistance", "GetAngle", "GetString", "GetKeywords",
        "GetInteger", "GetDouble", "GetEntity", "GetNestedEntity", "GetSelection",
        "GetFileNameForOpen", "GetFileNameForSave", "Drag");

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Service/ViewModel không được hỏi người dùng qua dòng lệnh",
        messageFormat: "Lớp '{0}' nằm trong {1} — gọi {2} (prompt dòng lệnh). Hỏi người dùng ở tầng Command " +
                       "rồi truyền kết quả vào Service; ViewModel nhận dữ liệu qua binding.",
        category: "CadCoreFramework.Architecture",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Prompt trong Service/ViewModel khiến chúng không dùng được từ Palette, batch hay unit test, " +
                     "và trộn việc thu thập input của Command với nghiệp vụ.",
        helpLinkUri: "https://github.com/Tmphi/CadCoreFramework/blob/main/docs/training/T09_SENIOR_PLAYBOOK.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        INamedTypeSymbol? target = method.ContainingType;
        if (target is null) return;

        string? api = null;
        if (target.Name == "CadPromptHelper")
            api = "CadPromptHelper." + method.Name;
        else if (target.ToDisplayString() == "Autodesk.AutoCAD.EditorInput.Editor" && EditorPromptMethods.Contains(method.Name))
            api = "Editor." + method.Name;
        if (api is null) return;

        INamedTypeSymbol? caller = context.ContainingSymbol as INamedTypeSymbol ?? context.ContainingSymbol?.ContainingType;
        string? tier = ArchitectureLayers.TierOf(caller);
        if (tier is null) return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), caller!.Name, tier, api));
    }
}
