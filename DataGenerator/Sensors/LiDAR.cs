using System.ComponentModel;
using DataGenerator.Enums;
using DataGenerator.Environment;

namespace DataGenerator.Sensors;

public class LiDAR : Sensor
{
    private const int MAX_DISTANCE = 10;

    public int? GetDistance(Map map, (int X, int Y) realPosition, Orientation orientation)
    {
        (int xDirection, int yDirection) = orientation switch
        {
            Orientation.Up => (0, 1),
            Orientation.Down => (0, -1),
            Orientation.Left => (-1, 0),
            Orientation.Right => (1, 0),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        int visibility_treshold = map.GetTemperature(realPosition) > 100 ? MAX_DISTANCE / 2 : MAX_DISTANCE;

        int distance = 1;
        var actualPosition = (realPosition.X + distance * xDirection, realPosition.Y + distance * yDirection);
        TerrainType terrain = map.GetTerrainType(actualPosition);
        while (terrain == TerrainType.Free || terrain == TerrainType.Fire)
        {
            distance++;
            actualPosition = (realPosition.X + distance * xDirection, realPosition.Y + distance * yDirection);

            if (distance > visibility_treshold || !map.IsInsideMap(actualPosition))
            {
                return null;
            }
            terrain = map.GetTerrainType(actualPosition);
        }

        return DamageLevel switch
        {
            DamageLevel.Ok => distance + Tools.RandomGaussianInt(0, 0.5),
            DamageLevel.Damaged => distance + Tools.RandomGaussianInt(0, 0.8),
            DamageLevel.Destroyed => null,
            _ => throw new InvalidEnumArgumentException("Unknown damage level")
        };
    }
}
