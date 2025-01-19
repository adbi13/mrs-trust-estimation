using System.ComponentModel;
using DataGenerator.Enums;

namespace DataGenerator.Sensors;

public class IMU : Sensor
{
    public Orientation? GetOrientation(Orientation realOrientation)
    {
        double? noise = DamageLevel switch
        {
            DamageLevel.Ok => Tools.RandomGaussianDouble(0, 0.25),
            DamageLevel.Damaged => Tools.RandomGaussianDouble(0, 1),
            DamageLevel.Destroyed => null,
            _ => throw new InvalidEnumArgumentException("Unknown damage level")
        };

        return noise switch
        {
            null => null,
            < -1 => realOrientation switch
                {
                    Orientation.Up => Orientation.Left,
                    Orientation.Left => Orientation.Down,
                    Orientation.Down => Orientation.Right,
                    Orientation.Right => Orientation.Up,
                    _ => throw new InvalidEnumArgumentException("Unknown orientation")
                },
            < 1 => realOrientation,
            _ => realOrientation switch
                 {
                     Orientation.Down => Orientation.Left,
                     Orientation.Right => Orientation.Down,
                     Orientation.Up => Orientation.Right,
                     Orientation.Left => Orientation.Up,
                     _ => throw new InvalidEnumArgumentException("Unknown orientation")
                 },
        };
    }
}
