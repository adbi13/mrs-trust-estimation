using System.Diagnostics;
using DataGenerator.Enums;

namespace DataGenerator.Environment;

public class Map
{
    private Point[][] _points;

    public (int X, int Y) Size { get; set; }

    private int _itemsCount = 0;

    private int _itemsCollected = 0;

    private int _itemsDestroyed = 0;

    private int _destroyedRobots = 0;

    private Map(uint xSize, uint ySize)
    {
        _points = new Point[xSize][];
        for (int x = 0; x < xSize; x++)
        {
            _points[x] = new Point[ySize];
        }
        Size = ((int) xSize, (int) ySize);
    }

    public int GetPositionId((int X, int Y) position)
    {
        Debug.Assert(IsInsideMap(position));

        return _points[position.X][position.Y].Id;
    }

    public TerrainType GetTerrainType((int X, int Y) position)
    {
        Debug.Assert(IsInsideMap(position));

        return _points[position.X][position.Y].TerrainType;
    }

    public float GetTemperature((int X, int Y) position)
    {
        Debug.Assert(IsInsideMap(position));

        return _points[position.X][position.Y].Temperature;
    }

    public int? GetOccupiedByRobotId((int X, int Y) position)
    {
        Debug.Assert(IsInsideMap(position));

        return _points[position.X][position.Y].OccupiedBy switch
        {
            Robot robot => robot.Id,
            _ => null
        };
    }

    public int? GetOccupiedByItemId((int X, int Y) position)
    {
        Debug.Assert(IsInsideMap(position));

        return _points[position.X][position.Y].OccupiedBy switch
        {
            Item item => item.Id,
            _ => null
        };
    }

    public int? GetBeaconCardinality((int X, int Y) position)
    {
        Debug.Assert(IsInsideMap(position));

        if (_points[position.X][position.Y].OccupiedBy is Robot robot
            && robot.Role == Role.Beacon)
        {
            return robot.Cardinality;
        }

        return null;
    }

    public bool IsInsideMap((int X, int Y) position)
    {
        return 0 <= position.X && position.X < Size.X
            && 0 <= position.Y && position.Y < Size.Y;
    }

    public bool MoveFromTo((int X, int Y) from, (int X, int Y) to)
    {
        if (to.X < 0 || to.X >= _points.Length || to.Y < 0 || to.Y >= _points[0].Length)
        {
            return false;
        }
        var toPoint = _points[to.X][to.Y];
        var fromPoint = _points[from.X][from.Y];
        if (toPoint.TerrainType == TerrainType.Free)
        {
            _points[to.X][to.Y].TerrainType = TerrainType.OccupiedByRobot;
            toPoint.OccupiedBy = fromPoint.OccupiedBy;
            fromPoint.TerrainType = TerrainType.Free;
            fromPoint.OccupiedBy = null;
            return true;
        }

        return false;
    }

    public Item? RemoveItem((int X, int Y) from)
    {
        if (_points[from.X][from.Y].OccupiedBy is Item item)
        {
            _points[from.X][from.Y].OccupiedBy = null;
            _points[from.X][from.Y].TerrainType = TerrainType.Free;
            return item;
        }

        return null;
    }

    public bool PlaceItem((int X, int Y) to, Item item)
    {
        if (_points[to.X][to.Y].TerrainType == TerrainType.Free)
        {
            _points[to.X][to.Y].OccupiedBy = item;
            _points[to.X][to.Y].TerrainType = TerrainType.OccupiedByItem;
            return true;
        }
        if (_points[to.X][to.Y].TerrainType == TerrainType.Base)
        {
            _itemsCollected++;
            return true;
        }
        return false;
    }

    public bool Destroy((int X, int Y) coordinates)
    {
        if (coordinates.X == 0 || coordinates.X == Size.X - 1
            || coordinates.Y == 0 || coordinates.Y == Size.Y - 1)
        {
            return false;
        }
        if (_points[coordinates.X][coordinates.Y].TerrainType == TerrainType.OccupiedByRobot
            || _points[coordinates.X][coordinates.Y].TerrainType == TerrainType.OccupiedByItem
            || _points[coordinates.X][coordinates.Y].TerrainType == TerrainType.Obstacle)
        {
            if (_points[coordinates.X][coordinates.Y].OccupiedBy is Robot robot)
            {
                robot.Alive = false;
                _destroyedRobots++;
            }
            if (_points[coordinates.X][coordinates.Y].OccupiedBy is Item)
            {
                _itemsDestroyed++;
            }
            _points[coordinates.X][coordinates.Y].OccupiedBy = null;
            _points[coordinates.X][coordinates.Y].TerrainType = TerrainType.Free;
            return true;
        }
        return false;
    }

    public bool SetOnFire((int X, int Y) coordinates)
    {
        if (_points[coordinates.X][coordinates.Y].TerrainType == TerrainType.Base)
        {
            return false;
        }

        if (_points[coordinates.X][coordinates.Y].OccupiedBy is Robot robot)
        {
            robot.Alive = false;
            _destroyedRobots++;
        }

        _points[coordinates.X][coordinates.Y].OccupiedBy = null;
        _points[coordinates.X][coordinates.Y].TerrainType = TerrainType.Fire;
        return true;
    }

    private void FloodFillObstacles(uint x, uint y, ref int obstaclesToAdd,
        TerrainType terrainType, Random random, double nextProbability)
    {
        if (obstaclesToAdd <= 0 || x < 0 || y < 0 || x >= _points.Length || y >= _points[0].Length)
        {
            return;
        }

        if (random.NextDouble() < nextProbability)
        {
            _points[x][y].TerrainType = terrainType;
            if (terrainType == TerrainType.Fire)
            {
                _points[x][y].Temperature = (float) random.NextDouble() * 100f + 300f;
            }
            obstaclesToAdd--;
            FloodFillObstacles(x + 1, y, ref obstaclesToAdd, terrainType, random, nextProbability / 2);
            FloodFillObstacles(x, y + 1, ref obstaclesToAdd, terrainType, random, nextProbability / 2);
            FloodFillObstacles(x - 1, y, ref obstaclesToAdd, terrainType, random, nextProbability / 2);
            FloodFillObstacles(x, y - 1, ref obstaclesToAdd, terrainType, random, nextProbability / 2);
        }
    }

    public static Map GenerateRandom(uint xSize, uint ySize, int seed,
        double itemProbability=0.05, double obstacleCoverage=0.2)
    {
        var map = new Map(xSize, ySize);
        var random = new Random(seed);

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                map._points[x][y] = new()
                {
                    Temperature = (float) random.NextDouble() * 100f + 18f
                };
            }
        }

        var obstaclesToAdd = (int) (xSize * ySize * obstacleCoverage);
        while (obstaclesToAdd > 0)
        {
            var x = (uint) random.Next((int) xSize);
            var y = (uint) random.Next((int) ySize);

            var terrainType = random.NextDouble() < 0.3 ? TerrainType.Fire : TerrainType.Obstacle;

            map.FloodFillObstacles(x, y, ref obstaclesToAdd, terrainType, random, 0.8);
        }

        for (int x = 0; x < xSize; x++)
        {
            map._points[x][0].TerrainType = random.NextDouble() < 0 ? TerrainType.Base : TerrainType.Obstacle;
            map._points[x][ySize - 1].TerrainType = random.NextDouble() < 0 ? TerrainType.Base : TerrainType.Obstacle;
        }

        for (int y = 0; y < ySize; y++)
        {
            map._points[0][y].TerrainType = random.NextDouble() < 0 ? TerrainType.Base : TerrainType.Obstacle;
            map._points[xSize - 1][y].TerrainType = random.NextDouble() < 0 ? TerrainType.Base : TerrainType.Obstacle;
        }

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                var point = map._points[x][y];
                if (point.TerrainType == TerrainType.Free && random.NextDouble() < itemProbability)
                {
                    point.OccupiedBy = new Item();
                    point.TerrainType = TerrainType.OccupiedByItem;
                    map._itemsCount++;
                }
            }
        }

        map._points[random.Next((int) xSize)][random.Next((int) ySize)].TerrainType = TerrainType.Base;

        return map;
    }

    public void PlaceRobots(IEnumerable<Robot> robots, int seed)
    {
        var random = new Random(seed);
        var freePoints = new PriorityQueue<(Point, (int, int)), int>(
            _points
            .SelectMany((ps, x) => ps.Select((p, y) => (p, (x, y))))
            .Where(p => p.Item1.TerrainType == TerrainType.Free)
            .Select(p => (p, random.Next())));
        foreach (var robot in robots)
        {
            var point = freePoints.Dequeue();
            point.Item1.OccupiedBy = robot;
            point.Item1.TerrainType = TerrainType.OccupiedByRobot;
            robot.__Position = point.Item2;
            robot.__Map = this;
        }
    }

    public void Print()
    {
        for (int y = _points[0].Length - 1; y >= 0; y--)
        {
            for (int x = 0; x < _points.Length; x++)
            {
                Console.BackgroundColor = _points[x][y].TerrainType switch
                {
                    TerrainType.Obstacle => ConsoleColor.DarkYellow,
                    TerrainType.Fire => ConsoleColor.DarkRed,
                    TerrainType.Free => ConsoleColor.White,
                    TerrainType.Base => ConsoleColor.Cyan,
                    TerrainType.OccupiedByItem => ConsoleColor.Green,
                    _ => ConsoleColor.White,
                };
                if (_points[x][y].OccupiedBy is Robot robot)
                {
                    if (robot.HoldingItem is not null)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkGreen;
                    }
                    Console.ForegroundColor = robot.Profile switch
                    {
                        RobotProfile.Arsonist => ConsoleColor.Red,
                        RobotProfile.ItemDestroyer => ConsoleColor.Yellow,
                        RobotProfile.Liar => ConsoleColor.Blue,
                        RobotProfile.Broken => ConsoleColor.Magenta,
                        _ => ConsoleColor.Black
                    };
                }
                Console.Write(
                    _points[x][y].TerrainType switch
                    {
                        TerrainType.OccupiedByRobot when _points[x][y].OccupiedBy is Robot r =>
                            r.__Orientation switch
                            {
                                Orientation.Up => $"⮝{(r.Cardinality.HasValue ? r.Cardinality.ToString() : " ")}",
                                Orientation.Down => $"⮟{(r.Cardinality.HasValue ? r.Cardinality.ToString() : " ")}",
                                Orientation.Left => $"⮜{(r.Cardinality.HasValue ? r.Cardinality.ToString() : " ")}",
                                Orientation.Right => $"⮞{(r.Cardinality.HasValue ? r.Cardinality.ToString() : " ")}",
                                _ => "? "
                            },
                        _ => "  "
                    });
                Console.ResetColor();
            }
            Console.WriteLine();
        }
        Console.WriteLine($"{_itemsCollected} / ({_itemsCount} - {_itemsDestroyed}) items collected");
        Console.WriteLine($"{_destroyedRobots} robots destroyed");
    }

    public void LogDym()
    {
        for (int y = _points[0].Length - 1; y >= 0; y--)
        {
            for (int x = 0; x < _points.Length; x++)
            {
                File.AppendAllText("dym_map_point.tsv", $"{_points[x][y].Id}\t{x}\t{y}\t{_points[x][y].Temperature}\n");
                if (_points[x][y].OccupiedBy is Item item)
                {
                    File.AppendAllText("dym_item.tsv", $"{item.Id}\n");
                }
            }
        }
    }

    public void LogFact(uint time)
    {
        for (int y = _points[0].Length - 1; y >= 0; y--)
        {
            for (int x = 0; x < _points.Length; x++)
            {
                _points[x][y].Log(time);
            }
        }
    }
}
