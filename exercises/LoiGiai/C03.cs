// Lời giải mẫu C03 — giống đáp án trong docs/training/C03_OBJECT_ORIENTED.md §10.
namespace Exercises.C03;

// E1
public sealed class PanelSpec
{
    public const double MinWidth = 300;
    public const double MaxHeight = 2400;

    public string Name { get; }
    public double Width { get; }
    public double Height { get; }

    public PanelSpec(string name, double width, double height = 2000)
    {
        Name = name ?? string.Empty;
        Width = width;
        Height = height;
    }

    public double Area => Width * Height;
    public bool IsTall => Height > 2000;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Tên tủ không được để trống.");
        if (Width < MinWidth) errors.Add($"Chiều rộng phải ≥ {MinWidth}.");
        if (Height <= 0) errors.Add("Chiều cao phải > 0.");
        if (Height > MaxHeight) errors.Add($"Chiều cao phải ≤ {MaxHeight}.");
        return errors;
    }

    public bool IsValid => Validate().Count == 0;
}

// E2
public static class E2
{
    public static void Run()
    {
        Shape[] shapes = { new Rect(3, 4), new Circle(1), new Triangle(3, 4) };
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
    public Triangle(double baseLength, double height) : base("Tam giác")
    {
        BaseLength = baseLength;
        Height = height;
    }

    public double BaseLength { get; }
    public double Height { get; }
    public override double Area() => 0.5 * BaseLength * Height;
}

// E3
public interface IPanelRepository
{
    void Add(string name);
    IReadOnlyList<string> GetAll();
}

public sealed class InMemoryPanelRepository : IPanelRepository
{
    private readonly List<string> _names = new List<string>();
    public void Add(string name) => _names.Add(name);
    public IReadOnlyList<string> GetAll() => _names;
}

public sealed class PanelReport
{
    private readonly IPanelRepository _repository;
    public PanelReport(IPanelRepository repository) { _repository = repository; }
    public string Summary()
    {
        var names = _repository.GetAll();
        return $"{names.Count} tủ: {string.Join(", ", names)}";
    }
}
