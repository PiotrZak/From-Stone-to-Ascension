namespace TTS.Core.Systems;

/// <summary>Deterministic 2D value noise for hex map generation.</summary>
internal static class ValueNoise
{
    public static double Sample(int seed, double x, double y)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;
        var sx = x - x0;
        var sy = y - y0;

        var n00 = Hash(seed, x0, y0);
        var n10 = Hash(seed, x1, y0);
        var n01 = Hash(seed, x0, y1);
        var n11 = Hash(seed, x1, y1);

        var ix0 = Lerp(n00, n10, Smooth(sx));
        var ix1 = Lerp(n01, n11, Smooth(sx));
        return Lerp(ix0, ix1, Smooth(sy));
    }

    public static double Fbm(int seed, double x, double y, int octaves = 4)
    {
        var value = 0.0;
        var amplitude = 1.0;
        var frequency = 1.0;
        var total = 0.0;

        for (var i = 0; i < octaves; i++)
        {
            value += Sample(seed + i * 997, x * frequency, y * frequency) * amplitude;
            total += amplitude;
            amplitude *= 0.5;
            frequency *= 2.0;
        }

        return value / total;
    }

    private static double Hash(int seed, int x, int y)
    {
        unchecked
        {
            var h = seed;
            h = h * 31 + x;
            h = h * 31 + y;
            h ^= h >> 13;
            h *= 1274126177;
            return (h & 0x7FFFFFFF) / (double)int.MaxValue;
        }
    }

    private static double Smooth(double t) => t * t * (3 - 2 * t);

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
