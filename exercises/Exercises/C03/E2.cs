// C03-E2 — docs/training/C03_OBJECT_ORIENTED.md §10
// Ví dụ kế thừa của C03 §7 đã chép sẵn dưới đây. Việc của bạn:
//   1. hoàn thiện class Triangle (đáy, chiều cao; tên hình "Tam giác");
//   2. thêm new Triangle(3, 4) vào CUỐI mảng shapes trong Run(), để in thêm dòng
//      "Tam giác: diện tích 6.0".
// Thử trước: xóa dòng "public override double Area()" của Triangle rồi build, đọc lỗi CS0534,
// sau đó viết lại dòng đó cho đúng.
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C03.E2
namespace Exercises.C03;

public static class E2
{
    public static void Run()
    {
        Shape[] shapes = { new Rect(3, 4), new Circle(1) };   // TODO: thêm tam giác
        foreach (Shape s in shapes)
            Console.WriteLine(s.Describe());
    }
}

public abstract class Shape
{
    protected Shape(string name) { Name = name; }
    public string Name { get; }
    public abstract double Area();
    public virtual string Describe() => $"{Name}: diện tích {Area():F1}";
}

public sealed class Rect : Shape
{
    public Rect(double w, double h) : base("Chữ nhật") { W = w; H = h; }
    public double W { get; }
    public double H { get; }
    public override double Area() => W * H;
}

public sealed class Circle : Shape
{
    public Circle(double r) : base("Tròn") { R = r; }
    public double R { get; }
    public override double Area() => Math.PI * R * R;
    public override string Describe() => "⭕ " + base.Describe();
}

public sealed class Triangle : Shape
{
    // TODO: tên hình đúng là "Tam giác".
    public Triangle(double baseLength, double height) : base("TODO")
    {
        BaseLength = baseLength;
        Height = height;
    }

    public double BaseLength { get; }
    public double Height { get; }

    // TODO: viết bài làm ở đây, thay cho throw.
    public override double Area() => throw new NotImplementedException();
}
