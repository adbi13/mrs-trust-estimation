using DataGenerator.Enums;

namespace DataGenerator.Environment;

public class Point
{
    private static int _idInit = 0;

    public int Id { get; set; } = _idInit++;

    public TerrainType TerrainType { get; set; } = TerrainType.Free;

    public IBody? OccupiedBy { get; set; }

    public float Temperature { get; set; }

    public void Log(uint time)
    {
        int? occupiedByItemId = null;
        int? occupiedByRobotId = null;
        if (OccupiedBy is Robot robot)
        {
            occupiedByRobotId = robot.Id;
        }
        else if (OccupiedBy is Item item)
        {
            occupiedByItemId = item.Id;
        }
        File.AppendAllText("fact_map_point_state.tsv", $"{time}\t{Id}\t{TerrainType}\t{occupiedByItemId}\t{occupiedByRobotId}\n");
    }
}
