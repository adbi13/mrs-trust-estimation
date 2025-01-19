using DataGenerator.Enums;

namespace DataGenerator.Environment;

public class MemoryPoint
{
    public TerrainType TerrainType { get; set; } = TerrainType.Free;

    public int RobotId { get; set; }

    public int? OccupiedByItemId { get; set; } = null;

    public int? OccupiedByRobotId { get; set; } = null;

    public (int X, int Y) Position { get; set; }

    public void Log(uint time)
    {
        File.AppendAllText("fact_memory_map_point.tsv", $"{time}\t{Position.X}\t{Position.Y}\t{RobotId}\t{TerrainType}\t{OccupiedByItemId}\t{OccupiedByRobotId}\n");
    }
}
