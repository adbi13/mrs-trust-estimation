using DataGenerator.Enums;

namespace DataGenerator.Records;

public record StepRecord
{
    public uint StartTime { get; set; }
    public int RobotId { get; set; }
    public int FromPositionId { get; set; }
    public int ToPositionId { get; set; }
    public int? MeasuredGpsX { get; set; }
    public int? MeasuredGpsY { get; set; }
    public int __RealGpsX { get; set; }
    public int __RealGpsY { get; set; }
    public Orientation? MeasuredOrientation { get; set; }
    public Orientation __RealOrientation { get; set; }
    public int? MeasuredLidarDistance { get; set; }
    public int? MeasuredRadarDistance { get; set; }
    public int __RealDistance { get; set; }
    public float? MeasuredTemperature { get; set; }
    public float __RealTemperature { get; set; }
    public string DecidedAction { get; set; } = "";
    public bool ActionSuccessful { get; set; }
    public int? HoldingItemId { get; set; }
    public int? Cardinality { get; set; }

    public void Log()
    {
        File.AppendAllText("fact_step.tsv", $"{StartTime}\t{RobotId}\t{FromPositionId}\t{ToPositionId}\t{MeasuredGpsX}\t{MeasuredGpsY}\t{__RealGpsX}\t{__RealGpsY}\t{MeasuredOrientation}\t{__RealOrientation}\t{MeasuredLidarDistance}\t{MeasuredRadarDistance}\t{__RealDistance}\t{MeasuredTemperature}\t{__RealTemperature}\t{DecidedAction}\t{ActionSuccessful}\t{HoldingItemId}\t{Cardinality}\n");
    }
}
