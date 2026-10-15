// C04-E3 — Event — docs/training/C04_CSHARP_IN_THIS_PROJECT.md §13
// DrawJob có event EventHandler<int>? Progress. Run(steps) phát tiến độ phần trăm (số nguyên)
// sau mỗi bước: Run(4) phát 25, 50, 75, 100. Không ai đăng ký thì Run vẫn chạy bình thường.
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C04.E3
namespace Exercises.C04;

public sealed class DrawJob
{
    // TODO: thay dòng dưới bằng khai báo event thông thường:
    //       public event EventHandler<int>? Progress;
    public event EventHandler<int>? Progress { add { } remove { } }

    public void Run(int steps)
    {
        // TODO: viết bài làm ở đây, thay cho dòng throw.
        throw new NotImplementedException();
    }
}
