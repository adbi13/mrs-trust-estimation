using MathNet.Numerics.Distributions;

public static class Tools
{
    private static Random _random = new(29);

    public static void SetSeed(int seed)
    {
        _random = new(seed);
    }

    public static int RandomGaussianInt(double mean, double stddev)
    {
        var normalDistribution = new Normal(mean, stddev, _random);
        return (int) normalDistribution.Sample();
    }

    public static double RandomGaussianDouble(double mean, double stddev)
    {
        var normalDistribution = new Normal(mean, stddev, _random);
        return normalDistribution.Sample();
    }

    public static float RandomFloat()
    {
        return _random.NextSingle();
    }

    public static (int X, int Y) RandomDirection()
    {
        return _random.NextDouble() switch
        {
            < 0.3 => (0, 1),
            < 0.6 => (-1, 0),
            < 0.9 => (0, 1),
            _ => (0, -1)
        };
    }
}
