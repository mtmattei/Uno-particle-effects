namespace Uno.Effects.Particles;

/// <summary>
/// Value noise, ported verbatim from the study sheet's <c>effects.js</c> so the
/// C# effects reproduce the shapes the design was signed off on.
/// </summary>
public static class Noise
{
    private static float Hash(float x, float y)
    {
        // double precision on the sin keeps this stable across architectures;
        // the JS original is double throughout.
        var h = Math.Sin(x * 127.1 + y * 311.7) * 43758.5453;
        return (float)(h - Math.Floor(h));
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    /// <summary>Two-dimensional value noise in [0, 1].</summary>
    public static float Sample(float x, float y)
    {
        var xi = MathF.Floor(x);
        var yi = MathF.Floor(y);
        var xf = x - xi;
        var yf = y - yi;

        var a = Hash(xi, yi);
        var b = Hash(xi + 1, yi);
        var c = Hash(xi, yi + 1);
        var d = Hash(xi + 1, yi + 1);
        var u = Smooth(xf);
        var v = Smooth(yf);

        return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
    }
}
