using System.ComponentModel;
using DataGenerator.Enums;
using DataGenerator.Environment;
using DataGenerator.Records;

namespace DataGenerator.Sensors;

public class Camera : Sensor
{
    private const int MAX_DISTANCE = 4;

    public CameraViewPoint[]
        GetView(Map map, (int X, int Y) realPosition, Orientation realOrientation)
    {
        var viewPoints = Enumerable.Range(1, MAX_DISTANCE)
            .SelectMany(y => Enumerable.Range(-y, y * 2 + 1)
                .Select(x => new {
                    X = x,
                    Y = y
                }))
            .Select(coordinates => realOrientation switch
            {
                Orientation.Up => coordinates,
                Orientation.Down => new {X = -coordinates.X, Y = -coordinates.Y},
                Orientation.Left => new {X = -coordinates.Y, Y = coordinates.X},
                Orientation.Right => new {X = coordinates.Y, Y = -coordinates.X},
                _ => throw new InvalidEnumArgumentException("Unknown orientation")
            })
            .Where(coordinates => 0 <= realPosition.X + coordinates.X
                && realPosition.X + coordinates.X < map.Size.X
                && 0 <= realPosition.Y + coordinates.Y
                && realPosition.Y + coordinates.Y < map.Size.Y)
            .Select(coordinates => new CameraViewPoint
            {
                RelativeX = coordinates.X,
                RelativeY = coordinates.Y,
                TerrainType = map.GetTerrainType((realPosition.X + coordinates.X,
                    realPosition.Y + coordinates.Y)),
                OccupiedByRobotId = map.GetOccupiedByRobotId((realPosition.X + coordinates.X,
                    realPosition.Y + coordinates.Y)),
                OccupiedByItemId = map.GetOccupiedByItemId((realPosition.X + coordinates.X,
                    realPosition.Y + coordinates.Y)),
            });

        var invisiblePoints = new HashSet<(int, int)>();
        (int X, int Y) directionVector = realOrientation switch
        {
            Orientation.Up => (0, 1),
            Orientation.Down => (0, -1),
            Orientation.Left => (-1, 0),
            Orientation.Right => (1, 0),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        foreach (var viewPoint in viewPoints)
        {
            if (viewPoint.TerrainType == TerrainType.Free)
            {
                continue;
            }

            var side = (directionVector.X * viewPoint.RelativeY
                - directionVector.Y * viewPoint.RelativeX) switch
                {
                    < 0 => 1,
                    0 => 0,
                    > 0 => -1
                };

            var overshadowedPoints = Enumerable.Range(viewPoint.RelativeY + 1, MAX_DISTANCE)
                .SelectMany(y => Enumerable.Range(viewPoint.RelativeX, y + 1 - viewPoint.RelativeY)
                    .Select(x => new {X = x * side, Y = y}))
                .Select(coordinates => realOrientation switch
                {
                    Orientation.Up => coordinates,
                    Orientation.Down => new {
                        X = viewPoint.RelativeX - (coordinates.X - viewPoint.RelativeX),
                        Y = viewPoint.RelativeY - (coordinates.Y - viewPoint.RelativeY)
                    },
                    Orientation.Left => new {
                        X = viewPoint.RelativeX - (coordinates.Y - viewPoint.RelativeY),
                        Y = viewPoint.RelativeY + (coordinates.X - viewPoint.RelativeX)
                    },
                    Orientation.Right => new {
                        X = viewPoint.RelativeX + (coordinates.Y - viewPoint.RelativeY),
                        Y = viewPoint.RelativeY - (coordinates.X - viewPoint.RelativeX)
                    },
                    _ => throw new InvalidEnumArgumentException("Unknown orientation")
                })
                .Where(coordinates => coordinates.X != viewPoint.RelativeX
                    || coordinates.Y != viewPoint.RelativeY)
                .Select(coordinates => (coordinates.X, coordinates.Y));

            invisiblePoints.UnionWith(overshadowedPoints);
        }

        return viewPoints
            .Where(point => !invisiblePoints.Contains((point.RelativeX, point.RelativeY)))
            .ToArray();
    }
}
