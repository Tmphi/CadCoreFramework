// Hai API của AcMgd (AutoCAD có giao diện) mà mã nguồn framework được link vào đây còn gọi tới.
// Project lab chỉ tham chiếu AcDbMgd + AcCoreMgd để nạp được trong accoreconsole, nên thay bằng bản
// tương đương. Chỉ có hiệu lực TRONG DLL lab: plugin vẫn dùng bản thật của AcMgd.
namespace Autodesk.AutoCAD.ApplicationServices
{
    /// <summary>
    /// CadContext gọi <c>doc.GetAcadDocument()</c> (COM, nằm trong AcMgd) chỉ khi có <c>undoMarkName</c>
    /// để gom các bước Undo. Ném lỗi ở đây thì CadContext bắt lấy, ghi cảnh báo và chạy tiếp không gom Undo,
    /// đúng như khi COM thất bại trong AutoCAD thật.
    /// </summary>
    internal static class DocumentExtension
    {
        public static object GetAcadDocument(this Document doc)
            => throw new System.NotSupportedException("DLL lab không có COM của AcMgd: không gom được bước Undo.");
    }

    /// <summary>
    /// Ở v1.0.0, <c>Utils/CadPromptHelper</c> và <c>Utils/CoordinateHelper</c> dùng <c>Application</c> của AcMgd
    /// (từ PR #6 chúng dùng <c>Core.Application</c>). Lớp này cho code cũ biên dịch được trên branch lab tạo từ v1.0.0;
    /// trên main, alias <c>using Application = …Core.Application;</c> trong từng file được ưu tiên hơn nó.
    /// </summary>
    internal static class Application
    {
        public static DocumentCollection DocumentManager => Core.Application.DocumentManager;
    }
}
