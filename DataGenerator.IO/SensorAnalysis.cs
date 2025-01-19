using System.ComponentModel;
using DataGenerator.Enums;
using DataGenerator.Environment;
using DataGenerator.Sensors;

namespace DataGenerator.IO;

public static class SensorAnalysis
{
    private static Random _random = new(13);

    public static void GPSAnalysis(int iterations)
    {
        const int MAP_WIDTH = 100;
        const int MAP_HEIGHT = 100;

        foreach (DamageLevel damageLevel in Enum.GetValues(typeof(DamageLevel)))
        {
            var gps = new GPS
            {
                DamageLevel = damageLevel
            };

            var correct = 0;
            var correct3x3 = 0;
            for (int i = 0; i < iterations; i++)
            {
                var position = (_random.Next(MAP_WIDTH), _random.Next(MAP_HEIGHT));
                var gpsPosition = gps.GetPosition(position);

                if (position == gpsPosition)
                {
                    correct++;
                }

                if (gpsPosition.HasValue
                    && Math.Abs(position.Item1 - gpsPosition.Value.X) <= 1
                    && Math.Abs(position.Item2 - gpsPosition.Value.Y) <= 1)
                {
                    correct3x3++;
                }
            }

            var accurancy = correct * 100f / iterations;
            var accurancy3x3 = correct3x3 * 100f / iterations;

            Console.WriteLine(damageLevel);
            Console.WriteLine($"\tCorrect: {correct}");
            Console.WriteLine($"\tWithin 3x3: {correct3x3}");
            Console.WriteLine($"\tAccurancy: {accurancy:0.00} %");
            Console.WriteLine($"\tAccurancy 3x3: {accurancy3x3:0.00} %");
        }

        var dummyCorrect = 0;
        var dummyCorrect3x3 = 0;
        for (int i = 0; i < iterations; i++)
        {
            var position = (_random.Next(MAP_WIDTH), _random.Next(MAP_HEIGHT));
            var gpsPosition = (_random.Next(MAP_WIDTH), _random.Next(MAP_HEIGHT));

            if (position == gpsPosition)
            {
                dummyCorrect++;
            }

            if (Math.Abs(position.Item1 - gpsPosition.Item1) <= 1
                && Math.Abs(position.Item2 - gpsPosition.Item2) <= 1)
            {
                dummyCorrect3x3++;
            }
        }

        var dummyAccurancy = dummyCorrect * 100f / iterations;
        var dummyAccurancy3x3 = dummyCorrect3x3 * 100f / iterations;

        Console.WriteLine("Dummy Baseline");
        Console.WriteLine($"\tCorrect: {dummyCorrect}");
        Console.WriteLine($"\tWithin 3x3: {dummyCorrect3x3}");
        Console.WriteLine($"\tAccurancy: {dummyAccurancy:0.00} %");
        Console.WriteLine($"\tAccurancy 3x3: {dummyAccurancy3x3:0.00} %");
    }

    public static void IMUAnalysis(int iterations)
    {
        var orientations = Enum.GetValues(typeof(Orientation));

        foreach (DamageLevel damageLevel in Enum.GetValues(typeof(DamageLevel)))
        {
            var imu = new IMU
            {
                DamageLevel = damageLevel
            };

            var correct = 0;
            for (int i = 0; i < iterations; i++)
            {
                var orientation = (Orientation) orientations.GetValue(_random.Next(orientations.Length))!;
                var imuOrientation = imu.GetOrientation(orientation);

                if (orientation == imuOrientation)
                {
                    correct++;
                }
            }

            var accurancy = correct * 100f / iterations;

            Console.WriteLine(damageLevel);
            Console.WriteLine($"\tCorrect: {correct}");
            Console.WriteLine($"\tAccurancy: {accurancy:0.00} %");
        }

        var dummyCorrect = 0;
        for (int i = 0; i < iterations; i++)
        {
            var orientation = (Orientation) orientations.GetValue(_random.Next(orientations.Length))!;
            var imuOrientation = (Orientation) orientations.GetValue(_random.Next(orientations.Length))!;

            if (orientation == imuOrientation)
            {
                dummyCorrect++;
            }
        }

        var dummyAccurancy = dummyCorrect * 100f / iterations;

        Console.WriteLine("Dummy Baseline");
        Console.WriteLine($"\tCorrect: {dummyCorrect}");
        Console.WriteLine($"\tAccurancy: {dummyAccurancy:0.00} %");
    }

    public static void ThermometerAnalysis(int iterations)
    {
        foreach (DamageLevel damageLevel in Enum.GetValues(typeof(DamageLevel)))
        {
            var thermometer = new Thermometer
            {
                DamageLevel = damageLevel
            };

            var correct = 0;
            for (int i = 0; i < iterations; i++)
            {
                var temperature = (float) _random.NextDouble() * 489.2f - 89.2f;
                var thermometerTemperature = thermometer.GetTemperature(temperature);

                if (thermometerTemperature == null)
                {
                    continue;
                }

                if (float.Abs(temperature - thermometerTemperature.Value) <= 1f)
                {
                    correct++;
                }
            }

            var accurancy = correct * 100f / iterations;

            Console.WriteLine(damageLevel);
            Console.WriteLine($"\tCorrect: {correct}");
            Console.WriteLine($"\tAccurancy: {accurancy:0.00} %");
        }

        var dummyCorrect = 0;
        for (int i = 0; i < iterations; i++)
        {
            var temperature = (float) _random.NextDouble() * 489.2f - 89.2f;
            var thermometerTemperature = (float) _random.NextDouble() * 489.2f - 89.2f;

            if (float.Abs(temperature - thermometerTemperature) <= 1f)
            {
                dummyCorrect++;
            }
        }

        var dummyAccurancy = dummyCorrect * 100f / iterations;

        Console.WriteLine("Dummy Baseline");
        Console.WriteLine($"\tCorrect: {dummyCorrect}");
        Console.WriteLine($"\tAccurancy: {dummyAccurancy:0.00} %");
    }

    private static int? GetRealDistance(Map map, (int X, int Y) realPosition, Orientation orientation)
    {
        const int MAX_DISTANCE = 10;
        (int xDirection, int yDirection) = orientation switch
        {
            Orientation.Up => (0, 1),
            Orientation.Down => (0, -1),
            Orientation.Left => (-1, 0),
            Orientation.Right => (1, 0),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        int distance = 1;
        TerrainType terrain = map.GetTerrainType((realPosition.X + distance * xDirection, realPosition.Y + distance * yDirection));
        while (terrain == TerrainType.Free || terrain == TerrainType.Fire)
        {
            distance++;
            if (distance > MAX_DISTANCE)
            {
                return null;
            }
            terrain = map.GetTerrainType((realPosition.X + distance * xDirection, realPosition.Y + distance * yDirection));
        }
        return distance;
    }

    public static void LiDARAnalysis(int iterations)
    {
        var map = Map.GenerateRandom(40, 40, 13);

        foreach (DamageLevel damageLevel in Enum.GetValues(typeof(DamageLevel)))
        {
            var lidar = new LiDAR
            {
                DamageLevel = damageLevel
            };

            var correct = 0;
            var hotPlaces = 0;
            var hotCorrect = 0;
            for (int i = 0; i < iterations; i++)
            {
                var position = (_random.Next(1, 39), _random.Next(1, 39));
                var orientation = _random.Next(1, 5) switch
                {
                    1 => Orientation.Down,
                    2 => Orientation.Left,
                    3 => Orientation.Right,
                    4 => Orientation.Up,
                    _ => Orientation.Up
                };

                var squareTemperature = map.GetTemperature(position);

                if (squareTemperature > 100)
                {
                    hotPlaces++;
                }

                var lidarDistance = lidar.GetDistance(map, position, orientation);

                if (lidarDistance == GetRealDistance(map, position, orientation))
                {
                    correct++;
                    if (squareTemperature > 100)
                    {
                        hotCorrect++;
                    }
                }
            }

            var accurancy = correct * 100f / iterations;
            var hotAccurancy = hotCorrect * 100f / hotPlaces;

            Console.WriteLine(damageLevel);
            Console.WriteLine($"\tCorrect: {correct}");
            Console.WriteLine($"\tAccurancy: {accurancy:0.00} %");
            Console.WriteLine($"\tHot Places: correct {hotCorrect}/{hotPlaces} {hotAccurancy:0.00} %");
        }

        var dummyCorrect = 0;
        for (int i = 0; i < iterations; i++)
        {
            var distance = _random.Next(1, 11);
            var lidarDistance = _random.Next(1, 11);

            if (distance == lidarDistance)
            {
                dummyCorrect++;
            }
        }

        var dummyAccurancy = dummyCorrect * 100f / iterations;

        Console.WriteLine("Dummy Baseline");
        Console.WriteLine($"\tCorrect: {dummyCorrect}");
        Console.WriteLine($"\tAccurancy: {dummyAccurancy:0.00} %");
    }

    public static void RadarAnalysis(int iterations)
    {
        var map = Map.GenerateRandom(40, 40, 13);

        foreach (DamageLevel damageLevel in Enum.GetValues(typeof(DamageLevel)))
        {
            var radar = new Radar
            {
                DamageLevel = damageLevel
            };

            var correct = 0;
            var hotPlaces = 0;
            var hotCorrect = 0;
            for (int i = 0; i < iterations; i++)
            {
                var position = (_random.Next(1, 39), _random.Next(1, 39));
                var orientation = _random.Next(1, 5) switch
                {
                    1 => Orientation.Down,
                    2 => Orientation.Left,
                    3 => Orientation.Right,
                    4 => Orientation.Up,
                    _ => Orientation.Up
                };

                var squareTemperature = map.GetTemperature(position);

                if (squareTemperature > 100)
                {
                    hotPlaces++;
                }

                var radarDistance = radar.GetDistance(map, position, orientation);

                if (radarDistance == GetRealDistance(map, position, orientation))
                {
                    correct++;
                    if (squareTemperature > 100)
                    {
                        hotCorrect++;
                    }
                }
            }

            var accurancy = correct * 100f / iterations;
            var hotAccurancy = hotCorrect * 100f / hotPlaces;

            Console.WriteLine(damageLevel);
            Console.WriteLine($"\tCorrect: {correct}");
            Console.WriteLine($"\tAccurancy: {accurancy:0.00} %");
            Console.WriteLine($"\tHot Places: correct {hotCorrect}/{hotPlaces} {hotAccurancy:0.00} %");
        }

        var dummyCorrect = 0;
        for (int i = 0; i < iterations; i++)
        {
            var distance = _random.Next(1, 11);
            var radarDistance = _random.Next(1, 11);

            if (distance == radarDistance)
            {
                dummyCorrect++;
            }
        }

        var dummyAccurancy = dummyCorrect * 100f / iterations;

        Console.WriteLine("Dummy Baseline");
        Console.WriteLine($"\tCorrect: {dummyCorrect}");
        Console.WriteLine($"\tAccurancy: {dummyAccurancy:0.00} %");
    }

    public static void RunAll(int iterations)
    {
        Console.WriteLine("IMU");
        Console.WriteLine("---");
        IMUAnalysis(iterations);
        Console.WriteLine();
        Console.WriteLine("GPS");
        Console.WriteLine("---");
        GPSAnalysis(iterations);
        Console.WriteLine();
        Console.WriteLine("Thermometer");
        Console.WriteLine("-----------");
        ThermometerAnalysis(iterations);
        Console.WriteLine();
        Console.WriteLine("LiDAR");
        Console.WriteLine("-----------");
        LiDARAnalysis(iterations);
        Console.WriteLine();
        Console.WriteLine("Radar");
        Console.WriteLine("-----------");
        RadarAnalysis(iterations);
        Console.WriteLine();
    }
}
