using System.ComponentModel;
using DataGenerator.Enums;

namespace DataGenerator.Sensors;

public class Thermometer : Sensor
{
    public float? GetTemperature(float realTemperature)
    {
        if (realTemperature < -50 || realTemperature > 100)
        {
            return DamageLevel switch
            {
                DamageLevel.Ok => realTemperature + (float) Tools.RandomGaussianDouble(0, 1.5),
                DamageLevel.Damaged => realTemperature + (float) Tools.RandomGaussianDouble(0, 6),
                DamageLevel.Destroyed => null,
                _ => throw new InvalidEnumArgumentException("Unknown damage level")
            };
        }
        return DamageLevel switch
        {
            DamageLevel.Ok => realTemperature + (float) Tools.RandomGaussianDouble(0, 0.3),
            DamageLevel.Damaged => realTemperature + (float) Tools.RandomGaussianDouble(0, 2),
            DamageLevel.Destroyed => null,
            _ => throw new InvalidEnumArgumentException("Unknown damage level")
        };
    }
}
