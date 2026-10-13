using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CadCoreFramework.Analyzers;

/// <summary>
/// CAD001 — Services và ViewModels không được truy cập Application.DocumentManager.MdiActiveDocument.
///
/// LÝ DO:
/// Service nằm trong tầng business logic và được inject ICadContext. Khi gọi
/// MdiActiveDocument trực tiếp:
///   • Document trỏ về thay đổi nếu user chuyển bản vẽ → vẽ nhầm file.
///   • Không được LockDocument tự động → eLockViolation.
///   • Phá vỡ unit test (FakeCadContext không giả lập được MdiActiveDocument).
///
/// Thay vì:
///     var doc = Application.DocumentManager.MdiActiveDocument;
/// Dùng:
///     _cadContext.Execute(scope => { var doc = scope.Document; ... });
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MdiActiveDocumentInServiceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CAD001";

    private static readonly LocalizableString Title =
        "Service/ViewModel không được dùng MdiActiveDocument";

    private static readonly LocalizableString MessageFormat =
        "Lớp '{0}' nằm trong {1} — truy cập Application.DocumentManager.MdiActiveDocument " +
        "trực tiếp. Hãy inject ICadContext và dùng scope.Document bên trong Execute lambda.";

    private static readonly LocalizableString Description =
        "Service và ViewModel phải thao tác với bản vẽ qua ICadContext để đảm bảo " +
        "LockDocument + Transaction + testability. Direct MdiActiveDocument access " +
        "là nguồn gốc của eLockViolation và flaky tests.";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: "CadCoreFramework.Architecture",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/Tmphi/CadCoreFramework/blob/main/docs/02_CORE_ARCHITECTURE.md"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        // Look for `.MdiActiveDocument` as the member name.
        if (memberAccess.Name.Identifier.ValueText != "MdiActiveDocument")
            return;

        // Walk up to the containing class to inspect its namespace / name.
        var classDecl = memberAccess.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDecl is null) return;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol is null) return;

        // Only flag classes inside Services or ViewModels namespaces, or whose name
        // ends with "Service"/"ViewModel". Commands + Core + Extensions legitimately
        // need MdiActiveDocument for infrastructure plumbing; CadContext (the ICadContext
        // implementation) is the one place MdiActiveDocument IS the right call.
        string? tier = ArchitectureLayers.TierOf(classSymbol);
        if (tier is null) return;

        var diagnostic = Diagnostic.Create(Rule, memberAccess.GetLocation(), classSymbol.Name, tier);
        context.ReportDiagnostic(diagnostic);
    }
}
