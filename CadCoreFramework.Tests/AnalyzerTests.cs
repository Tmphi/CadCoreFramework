using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CadCoreFramework.Analyzers;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// CAD001/CAD002/CAD003: biên dịch đoạn mã mẫu bằng Roslyn (kèm stub tối thiểu của các kiểu AutoCAD —
    /// analyzer nhận diện theo TÊN đầy đủ) rồi kiểm tra chẩn đoán. Không cần AutoCAD.
    /// </summary>
    public class AnalyzerTests
    {
        private const string Stubs = @"
namespace Autodesk.AutoCAD.DatabaseServices
{
    public enum OpenMode { ForRead, ForWrite, ForNotify }
    public struct ObjectId { }
    public class DBObject { public void UpgradeOpen() { } }
    public class Transaction { public DBObject GetObject(ObjectId id, OpenMode mode) => new DBObject(); }
}
namespace Autodesk.AutoCAD.EditorInput
{
    public class Editor
    {
        public object GetPoint(string message) => null;
        public object GetSelection() => null;
        public object GetCurrentView() => null;
    }
}
namespace Autodesk.AutoCAD.ApplicationServices
{
    public class Document { public Autodesk.AutoCAD.EditorInput.Editor Editor => null; }
    public class DocumentCollection { public Document MdiActiveDocument => null; }
    public static class Application { public static DocumentCollection DocumentManager => null; }
}
namespace Demo.Core
{
    public interface ICadScope
    {
        Autodesk.AutoCAD.DatabaseServices.Transaction Transaction { get; }
        Autodesk.AutoCAD.EditorInput.Editor Editor { get; }
    }
    public interface ICadContext
    {
        void ExecuteRead(System.Action<ICadScope> read);
        void Execute(System.Action<ICadScope> write);
    }
}
namespace Demo.Utils
{
    public static class CadPromptHelper { public static double? GetDouble(string message) => null; }
}";

        // ─── CAD002: prompt trong Service/ViewModel ─────────────────────────────

        [Fact]
        public void CAD002_PromptHelperInService_IsReported()
        {
            var ids = Analyze(@"
namespace Demo.Services
{
    public class GridService { public void Run() { Demo.Utils.CadPromptHelper.GetDouble(""spacing""); } }
}");
            ids.Should().Equal(PromptInServiceAnalyzer.DiagnosticId);
        }

        [Fact]
        public void CAD002_EditorPromptInViewModel_IsReported_EvenInsideLambda()
        {
            var ids = Analyze(@"
namespace Demo.ViewModels
{
    public class PickViewModel
    {
        public void Pick(Autodesk.AutoCAD.EditorInput.Editor ed)
        {
            System.Action a = () => ed.GetSelection();
            a();
        }
    }
}");
            ids.Should().Equal(PromptInServiceAnalyzer.DiagnosticId);
        }

        [Fact]
        public void CAD002_PromptInCommand_OrNonPromptEditorCallInService_IsNotReported()
        {
            var ids = Analyze(@"
namespace Demo.Commands
{
    public class GridCommands { public void Run(Autodesk.AutoCAD.EditorInput.Editor ed) { Demo.Utils.CadPromptHelper.GetDouble(""x""); ed.GetPoint(""p""); } }
}
namespace Demo.Services
{
    public class ViewService { public object Current(Autodesk.AutoCAD.EditorInput.Editor ed) => ed.GetCurrentView(); }
}");
            ids.Should().BeEmpty();
        }

        // ─── CAD003: ghi trong ExecuteRead ──────────────────────────────────────

        [Fact]
        public void CAD003_ForWriteInsideExecuteRead_IsReported()
        {
            var ids = Analyze(@"
using Autodesk.AutoCAD.DatabaseServices;
namespace Demo.Feature
{
    public class Reader
    {
        public void Read(Demo.Core.ICadContext ctx) =>
            ctx.ExecuteRead(scope => scope.Transaction.GetObject(default(ObjectId), OpenMode.ForWrite));
    }
}");
            ids.Should().Equal(WriteInExecuteReadAnalyzer.DiagnosticId);
        }

        [Fact]
        public void CAD003_UpgradeOpenInNestedLambdaInsideExecuteRead_IsReported()
        {
            var ids = Analyze(@"
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
namespace Demo.Feature
{
    public class Reader
    {
        public void Read(Demo.Core.ICadContext ctx, List<ObjectId> ids) =>
            ctx.ExecuteRead(scope => ids.ForEach(id => scope.Transaction.GetObject(id, OpenMode.ForRead).UpgradeOpen()));
    }
}");
            ids.Should().Equal(WriteInExecuteReadAnalyzer.DiagnosticId);
        }

        [Fact]
        public void CAD003_ForWriteInExecute_OrOutsideLambda_OrForReadInExecuteRead_IsNotReported()
        {
            var ids = Analyze(@"
using Autodesk.AutoCAD.DatabaseServices;
namespace Demo.Feature
{
    public class Writer
    {
        public void Write(Demo.Core.ICadContext ctx, Transaction tr)
        {
            ctx.Execute(scope => scope.Transaction.GetObject(default(ObjectId), OpenMode.ForWrite));
            tr.GetObject(default(ObjectId), OpenMode.ForWrite).UpgradeOpen();
            ctx.ExecuteRead(scope => scope.Transaction.GetObject(default(ObjectId), OpenMode.ForRead));
        }
    }
}");
            ids.Should().BeEmpty();
        }

        // ─── CAD001: hồi quy sau khi tách ArchitectureLayers ───────────────────

        [Fact]
        public void CAD001_MdiActiveDocumentInService_IsReported_ButNotInCadContext()
        {
            var ids = Analyze(@"
using Autodesk.AutoCAD.ApplicationServices;
namespace Demo.Services
{
    public class LayerService { public object Doc() => Application.DocumentManager.MdiActiveDocument; }
}
namespace Demo.Core
{
    public class CadContext { public object Doc() => Application.DocumentManager.MdiActiveDocument; }
}");
            ids.Should().Equal(MdiActiveDocumentInServiceAnalyzer.DiagnosticId);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static string[] Analyze(string source)
        {
            var compilation = CSharpCompilation.Create(
                "AnalyzerSample",
                new[] { CSharpSyntaxTree.ParseText(Stubs), CSharpSyntaxTree.ParseText(source) },
                References(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Mẫu phải biên dịch được — nếu không, test đang kiểm sai thứ.
            compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)
                .Should().BeEmpty("đoạn mã mẫu của test phải hợp lệ");

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
                new MdiActiveDocumentInServiceAnalyzer(), new PromptInServiceAnalyzer(), new WriteInExecuteReadAnalyzer());
            return compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult()
                .Select(d => d.Id)
                .ToArray();
        }

        private static IEnumerable<MetadataReference> References()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                typeof(object).Assembly.Location,
                typeof(Enumerable).Assembly.Location,
                typeof(List<>).Assembly.Location,
            };
            string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            foreach (string facade in new[] { "System.Runtime.dll", "netstandard.dll", "System.Collections.dll" })
            {
                string p = Path.Combine(runtimeDir, facade);
                if (File.Exists(p)) paths.Add(p);
            }
            return paths.Select(p => MetadataReference.CreateFromFile(p));
        }
    }
}
