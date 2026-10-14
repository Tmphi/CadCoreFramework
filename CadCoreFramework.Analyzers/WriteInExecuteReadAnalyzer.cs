using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CadCoreFramework.Analyzers;

/// <summary>
/// CAD003 — Không mở đối tượng để GHI (OpenMode.ForWrite / UpgradeOpen()) bên trong lambda truyền
/// cho ICadContext.ExecuteRead.
///
/// LÝ DO (docs/training/T09 B-07):
/// "Read-only" của ExecuteRead là HỢP ĐỒNG: transaction của nó kết thúc bằng Commit (rẻ hơn Abort) và
/// IsReadOnly chỉ chặn Append/XRecord của framework. Lambda tự GetObject(…, ForWrite) rồi sửa thì thay đổi
/// SẼ được ghi — và không có Undo mark, không lock trong command context. Rule này biến vi phạm hợp đồng
/// thành cảnh báo lúc build (lỗi khi build -warnaserror).
///
/// Giới hạn: chỉ thấy lời gọi trực tiếp trong lambda (kể cả lambda lồng); không theo vào method khác
/// được gọi từ lambda, và không xét method group (ExecuteRead(DocRead)).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WriteInExecuteReadAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CAD003";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Không mở đối tượng để ghi bên trong ExecuteRead",
        messageFormat: "{0} bên trong lambda của ExecuteRead — transaction của ExecuteRead được Commit nên thay đổi " +
                       "SẼ được ghi vào bản vẽ. Dùng Execute/TryExecute cho thao tác ghi.",
        category: "CadCoreFramework.Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ExecuteRead chỉ dành cho đọc. Mở ForWrite/UpgradeOpen trong đó làm thay đổi bản vẽ ngoài Undo mark " +
                     "và ngoài lock của Execute.",
        helpLinkUri: "https://github.com/Tmphi/CadCoreFramework/blob/main/docs/reference/R06_STANDARDS.md#cad003");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeFieldReference(OperationAnalysisContext context)
    {
        var reference = (IFieldReferenceOperation)context.Operation;
        IFieldSymbol field = reference.Field;
        if (field.Name != "ForWrite" ||
            field.ContainingType?.ToDisplayString() != "Autodesk.AutoCAD.DatabaseServices.OpenMode")
            return;

        if (IsInsideExecuteReadLambda(reference))
            context.ReportDiagnostic(Diagnostic.Create(Rule, reference.Syntax.GetLocation(), "OpenMode.ForWrite"));
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        if (method.Name != "UpgradeOpen" ||
            method.ContainingNamespace?.ToDisplayString() != "Autodesk.AutoCAD.DatabaseServices")
            return;

        if (IsInsideExecuteReadLambda(invocation))
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), "UpgradeOpen()"));
    }

    /// <summary>
    /// Đi ngược cây operation: đã đi qua ít nhất một lambda VÀ gặp lời gọi ExecuteRead của một
    /// *CadContext (ICadContext, CadContext, FakeCadContext…) → nằm trong lambda của ExecuteRead.
    /// </summary>
    private static bool IsInsideExecuteReadLambda(IOperation operation)
    {
        bool insideLambda = false;
        for (IOperation? current = operation.Parent; current != null; current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation)
            {
                insideLambda = true;
            }
            else if (insideLambda && current is IInvocationOperation call &&
                     call.TargetMethod.Name == "ExecuteRead" &&
                     call.TargetMethod.ContainingType?.Name.EndsWith("CadContext") == true)
            {
                return true;
            }
        }
        return false;
    }
}
