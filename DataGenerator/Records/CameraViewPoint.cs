using DataGenerator.Enums;

namespace DataGenerator.Records;

public record CameraViewPoint
{
    public int RelativeX { get; set; }
    public int RelativeY { get; set; }
    public TerrainType TerrainType { get; set; }
    public int? OccupiedByRobotId { get; set; }
    public int? OccupiedByItemId { get; set; }
}
