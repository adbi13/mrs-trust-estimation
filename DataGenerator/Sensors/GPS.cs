using System.ComponentModel;
using DataGenerator.Enums;

namespace DataGenerator.Sensors;

public class GPS : Sensor
{
    public (int X, int Y)? GetPosition((int X, int Y) realPosition)
    {
        return DamageLevel switch
        {
            DamageLevel.Ok => (realPosition.X + Tools.RandomGaussianInt(0, 0.8),
                               realPosition.Y + Tools.RandomGaussianInt(0, 0.8)),
            DamageLevel.Damaged => (realPosition.X + Tools.RandomGaussianInt(0, 1.7),
                                    realPosition.Y + Tools.RandomGaussianInt(0, 1.7)),
            DamageLevel.Destroyed => null,
            _ => throw new InvalidEnumArgumentException("Unknown damage level")
        };
    }
}
