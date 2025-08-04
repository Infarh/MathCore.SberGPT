using MathCore.Vectors;

namespace MathCore.SberGPT.ConsoleTest.Tests;

internal static class AreaTest
{
    public static void Run()
    {
        var points = new Vector2D[100000];
        var n = points.Length;
        const double radius = 1d;
        var w = 2 * Math.PI / n;
        for (var i = 0; i < n; i++)
            points[i] = new(radius * Math.Cos(w * i), radius * Math.Sin(w * i));

        var area = GetArea(points);

        const double expected_area = 3.141592651518704;
        if (Math.Abs(area - expected_area) > 1e-15)
            throw new($"Unexpected area: {area}");
    }

    /// <summary>Вычисляет площадь многоугольника по формуле Гаусса (Shoelace formula)</summary>
    /// <param name="Points">Последовательность вершин многоугольника</param>
    /// <returns>Площадь многоугольника</returns>
    private static double GetArea(IEnumerable<Vector2D> Points)
    {
        using var points = Points.GetEnumerator();
        if (!points.MoveNext())
            return 0;

        var p0 = points.Current;
        var p1 = p0;

        var area = 0d;
        while (points.MoveNext())
        {
            var (x1, y1) = points.Current;
            var (x2, y2) = p1;

            area += x2 * y1 - x1 * y2;
            p1 = points.Current;
        }

        area += p1.X * p0.Y - p0.X * p1.Y; // замыкаем контур

        return .5 * Math.Abs(area);
    }
}
