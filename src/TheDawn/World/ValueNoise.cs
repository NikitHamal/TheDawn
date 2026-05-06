namespace TheDawn.World;

public static class ValueNoise
{
    public static double Noise2D(int seed, double x, double y, double frequency)
    {
        x *= frequency;
        y *= frequency;
        var x0 = FastFloor(x);
        var y0 = FastFloor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;
        var sx = Smooth(x - x0);
        var sy = Smooth(y - y0);
        var n00 = HashUnit(seed, x0, y0);
        var n10 = HashUnit(seed, x1, y0);
        var n01 = HashUnit(seed, x0, y1);
        var n11 = HashUnit(seed, x1, y1);
        return Lerp(Lerp(n00, n10, sx), Lerp(n01, n11, sx), sy);
    }

    public static double Fractal(int seed, double x, double y, double frequency, int octaves, double persistence)
    {
        var amplitude = 1.0;
        var total = 0.0;
        var max = 0.0;
        for (var i = 0; i < octaves; i++)
        {
            total += Noise2D(seed + i * 7919, x, y, frequency) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= 2.0;
        }
        return total / max;
    }

    public static uint Hash(int seed, int x, int y, int salt = 0)
    {
        unchecked
        {
            uint h = 2166136261u;
            h = (h ^ (uint)seed) * 16777619u;
            h = (h ^ (uint)x) * 16777619u;
            h = (h ^ (uint)y) * 16777619u;
            h = (h ^ (uint)salt) * 16777619u;
            h ^= h >> 13;
            h *= 1274126177u;
            h ^= h >> 16;
            return h;
        }
    }

    public static double HashUnit(int seed, int x, int y, int salt = 0) => Hash(seed, x, y, salt) / (double)uint.MaxValue;

    private static int FastFloor(double value) => value >= 0 ? (int)value : (int)value - 1;
    private static double Smooth(double t) => t * t * (3.0 - 2.0 * t);
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
