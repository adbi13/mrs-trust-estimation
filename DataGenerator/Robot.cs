using System.ComponentModel;
using DataGenerator.Enums;
using DataGenerator.Environment;
using DataGenerator.Records;
using DataGenerator.Sensors;

namespace DataGenerator;

public class Robot : IBody
{
    private const int COMMUNICATION_RANGE = 8;

    private static int _idInit = 0;

    public int Id { get; set; } = _idInit++;

    public RobotProfile Profile { get; set; } = RobotProfile.Normal;

    public (int X, int Y) __Position { get; set; }

    public Map __Map { get; set; }

    public Orientation __Orientation { get; set; }

    private MemoryPoint?[][] _mapMemory;

    private GPS _gps = new();

    private IMU _imu = new();

    private Thermometer _thermometer = new();

    private LiDAR _lidar = new();

    private Radar _radar = new();

    private Camera _camera = new();

    public Item? HoldingItem { get; set; }

    public bool Alive { get; set; } = true;

    public Role Role { get; set; } = Role.Walker;

    public int? Cardinality { get; set; } = null;

    private bool _avoidingObstacle = false;

    public Robot(Map map, uint xSize, uint ySize,
        RobotProfile profile=RobotProfile.Normal)
    {
        __Map = map;
        _mapMemory = new MemoryPoint[xSize][];
        for (int x = 0; x < xSize; x++)
        {
            _mapMemory[x] = new MemoryPoint[ySize];
        }
        Profile = profile;
        if (profile == RobotProfile.Broken)
        {
            var randomFloat = Tools.RandomFloat();
            if (randomFloat < 0.2)
            {
                _gps.DamageLevel = DamageLevel.Damaged;
            }
            else if (randomFloat < 0.4)
            {
                _imu.DamageLevel = DamageLevel.Damaged;
            }
            else if (randomFloat < 0.6)
            {
                _radar.DamageLevel = DamageLevel.Damaged;
            }
            else if (randomFloat < 0.8)
            {
                _lidar.DamageLevel = DamageLevel.Damaged;
            }
            else
            {
                _thermometer.DamageLevel = DamageLevel.Damaged;
            }
        }
    }

    public bool StepForward()
    {
        var toPosition = __Orientation switch
        {
            Orientation.Up => (__Position.X, __Position.Y + 1),
            Orientation.Right => (__Position.X + 1, __Position.Y),
            Orientation.Down => (__Position.X, __Position.Y - 1),
            Orientation.Left => (__Position.X - 1, __Position.Y ),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        if (__Map.MoveFromTo(__Position, toPosition))
        {
            __Position = toPosition;
            return true;
        }

        return false;
    }

    public bool TurnRight()
    {
        __Orientation = __Orientation switch
        {
            Orientation.Up => Orientation.Right,
            Orientation.Right => Orientation.Down,
            Orientation.Down => Orientation.Left,
            Orientation.Left => Orientation.Up,
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };
        return true;
    }

    public bool TurnLeft()
    {
        __Orientation = __Orientation switch
        {
            Orientation.Up => Orientation.Left,
            Orientation.Right => Orientation.Up,
            Orientation.Down => Orientation.Right,
            Orientation.Left => Orientation.Down,
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };
        return true;
    }

    public bool GraspAnItem()
    {
        if (HoldingItem is not null)
        {
            return false;
        }

        var fromPosition = __Orientation switch
        {
            Orientation.Up => (__Position.X, __Position.Y + 1),
            Orientation.Right => (__Position.X + 1, __Position.Y),
            Orientation.Down => (__Position.X, __Position.Y - 1),
            Orientation.Left => (__Position.X - 1, __Position.Y ),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        HoldingItem = __Map.RemoveItem(fromPosition);

        return HoldingItem is not null;
    }

    public bool PutDownAnItem()
    {
        if (HoldingItem is null)
        {
            return false;
        }

        var toPosition = __Orientation switch
        {
            Orientation.Up => (__Position.X, __Position.Y + 1),
            Orientation.Right => (__Position.X + 1, __Position.Y),
            Orientation.Down => (__Position.X, __Position.Y - 1),
            Orientation.Left => (__Position.X - 1, __Position.Y ),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        if (__Map.PlaceItem(toPosition, HoldingItem))
        {
            HoldingItem = null;
            return true;
        }

        return false;
    }

    public bool Destroy()
    {
        var position = __Orientation switch
        {
            Orientation.Up => (__Position.X, __Position.Y + 1),
            Orientation.Right => (__Position.X + 1, __Position.Y),
            Orientation.Down => (__Position.X, __Position.Y - 1),
            Orientation.Left => (__Position.X - 1, __Position.Y ),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        return __Map.Destroy(position);
    }

    public bool StartFire()
    {
        var position = __Orientation switch
        {
            Orientation.Up => (__Position.X, __Position.Y + 1),
            Orientation.Right => (__Position.X + 1, __Position.Y),
            Orientation.Down => (__Position.X, __Position.Y - 1),
            Orientation.Left => (__Position.X - 1, __Position.Y ),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        return __Map.SetOnFire(position);
    }

    public bool DoNothing()
    {
        return true;
    }

    public void ReadSensors(StepRecord stepRecord)
    {
        var position = _gps.GetPosition(__Position);
        stepRecord.MeasuredGpsX = position?.X;
        stepRecord.MeasuredGpsY = position?.Y;
        stepRecord.MeasuredTemperature = _thermometer.GetTemperature(__Map.GetTemperature(__Position));
        stepRecord.MeasuredOrientation = _imu.GetOrientation(__Orientation);
        stepRecord.MeasuredRadarDistance = _radar.GetDistance(__Map, __Position, __Orientation);
        stepRecord.MeasuredLidarDistance = _lidar.GetDistance(__Map, __Position, __Orientation);
        if (position is not null)
        {
            foreach (var cameraView in _camera.GetView(__Map, __Position, __Orientation))
            {
                var capturedX = position.Value.X + cameraView.RelativeX;
                var capturedY = position.Value.Y + cameraView.RelativeY;
                if (0 <= capturedX && capturedX < _mapMemory.Length
                    && 0 <= capturedY && capturedY < _mapMemory[0].Length)
                {
                    _mapMemory[capturedX][capturedY] = new()
                    {
                        TerrainType = cameraView.TerrainType,
                        Position = (capturedX, capturedY),
                        OccupiedByItemId = cameraView.OccupiedByItemId,
                        OccupiedByRobotId = cameraView.OccupiedByRobotId,
                        RobotId = Id,
                    };
                }
            }
        }
    }

    private (int X, int Y)? GetFrontPosition(StepRecord stepInfo)
    {
        if (stepInfo.MeasuredGpsX.HasValue)
        {
            return stepInfo.MeasuredOrientation switch
            {
                Orientation.Up => (stepInfo.MeasuredGpsX.Value, stepInfo.MeasuredGpsY!.Value + 1),
                Orientation.Right => (stepInfo.MeasuredGpsX.Value + 1, stepInfo.MeasuredGpsY!.Value),
                Orientation.Down => (stepInfo.MeasuredGpsX.Value, stepInfo.MeasuredGpsY!.Value - 1),
                Orientation.Left => (stepInfo.MeasuredGpsX.Value - 1, stepInfo.MeasuredGpsY!.Value),
                null => (stepInfo.MeasuredGpsX.Value, stepInfo.MeasuredGpsY!.Value + 1),
                _ => throw new InvalidEnumArgumentException("Unknown orientation")
            };
        }
        return null;
    }

    private (int X, int Y)? GetLeftPosition(StepRecord stepInfo)
    {
        if (stepInfo.MeasuredGpsX.HasValue)
        {
            return stepInfo.MeasuredOrientation switch
            {
                Orientation.Up => (stepInfo.MeasuredGpsX.Value - 1, stepInfo.MeasuredGpsY!.Value),
                Orientation.Right => (stepInfo.MeasuredGpsX.Value, stepInfo.MeasuredGpsY!.Value + 1),
                Orientation.Down => (stepInfo.MeasuredGpsX.Value + 1, stepInfo.MeasuredGpsY!.Value),
                Orientation.Left => (stepInfo.MeasuredGpsX.Value, stepInfo.MeasuredGpsY!.Value - 1),
                null => (stepInfo.MeasuredGpsX.Value, stepInfo.MeasuredGpsY!.Value + 1),
                _ => throw new InvalidEnumArgumentException("Unknown orientation")
            };
        }
        return null;
    }

    private TerrainType DecideFrontPositionType(StepRecord stepInfo)
    {
        var frontPosition = GetFrontPosition(stepInfo);

        if (frontPosition is not null
            && frontPosition.Value.X >= 0
            && frontPosition.Value.X < _mapMemory.Length
            && frontPosition.Value.Y >= 0
            && frontPosition.Value.Y < _mapMemory[0].Length)
        {
            var memoryFrontPoint = _mapMemory[frontPosition.Value.X][frontPosition.Value.Y];
            if (memoryFrontPoint is not null
                && memoryFrontPoint.TerrainType == TerrainType.Free)
            {
                if ((stepInfo.MeasuredLidarDistance is null && stepInfo.MeasuredRadarDistance is null)
                || (stepInfo.MeasuredLidarDistance is not null && stepInfo.MeasuredLidarDistance > 1)
                || (stepInfo.MeasuredRadarDistance is not null && stepInfo.MeasuredRadarDistance > 1))
                {
                    return TerrainType.Free;
                }
                return TerrainType.OccupiedByRobot;
            }
            if (memoryFrontPoint is not null)
            {
                return memoryFrontPoint.TerrainType;
            }
        }

        if (stepInfo.MeasuredTemperature > 100 && stepInfo.MeasuredRadarDistance > 1)
        {
            return TerrainType.Free;
        }

        if (stepInfo.MeasuredTemperature <= 100 && stepInfo.MeasuredLidarDistance > 1)
        {
            return TerrainType.Free;
        }

        return TerrainType.OccupiedByRobot;
    }


    private (int X, int Y)? NearBase(StepRecord stepInfo)
    {
        if (!stepInfo.MeasuredGpsX.HasValue || !stepInfo.MeasuredGpsY.HasValue)
        {
            return null;
        }

        for (int x = int.Max(0, stepInfo.MeasuredGpsX.Value - COMMUNICATION_RANGE);
            x <= int.Min(_mapMemory.Length - 1, stepInfo.MeasuredGpsX.Value + COMMUNICATION_RANGE);
            x++)
        {
            for (int y = int.Max(0, stepInfo.MeasuredGpsY.Value - COMMUNICATION_RANGE);
                y <= int.Min(_mapMemory[0].Length - 1, stepInfo.MeasuredGpsY.Value + COMMUNICATION_RANGE);
                y++)
            {
                if (_mapMemory[x][y]?.TerrainType == TerrainType.Base)
                {
                    return (x, y);
                }
            }
        }

        return null;
    }


    private (int __X, int __Y, int Cardinality)[] GetNearbyBeacons()
    {
        List<(int __X, int __Y, int Cardinality)> beacons = new();
        for (int x = int.Max(0, __Position.X - COMMUNICATION_RANGE);
            x <= int.Min(__Map.Size.X - 1, __Position.X + COMMUNICATION_RANGE);
            x++)
        {
            for (int y = int.Max(0, __Position.Y - COMMUNICATION_RANGE);
                y <= int.Min(__Map.Size.Y - 1, __Position.Y + COMMUNICATION_RANGE);
                y++)
            {
                if (x == __Position.X && y == __Position.Y)
                {
                    continue;
                }

                int? cardinality = __Map.GetBeaconCardinality((x, y));

                if (cardinality != null)
                {
                   beacons.Add((x, y, cardinality.Value));
                }
            }
        }
        return beacons.ToArray();
    }


    private string StepApproachingPosition((int X, int Y) position, bool avoidCenter=true)
    {
        (int X, int Y) relativePosition =  __Orientation switch
        {
            Orientation.Up => position,
            Orientation.Down => (
                __Position.X - (position.X - __Position.X),
                __Position.Y - (position.Y - __Position.Y)
            ),
            Orientation.Right => (
                __Position.X - (position.Y - __Position.Y),
                __Position.Y + (position.X - __Position.X)
            ),
            Orientation.Left => (
                __Position.X + (position.Y - __Position.Y),
                __Position.Y - (position.X - __Position.X)
            ),
            _ => throw new InvalidEnumArgumentException("Unknown orientation")
        };

        if (avoidCenter
            && relativePosition.Y == __Position.Y
            && (relativePosition.X == __Position.X - 1
                || relativePosition.X == __Position.X + 1))
        {
            return nameof(StepForward);
        }
        else if (relativePosition.Y <= __Position.Y
            && relativePosition.X >= __Position.X)
        {
            return nameof(TurnRight);
        }
        else if (relativePosition.Y <= __Position.Y
            && relativePosition.X < __Position.X)
        {
            return nameof(TurnLeft);
        }
        return nameof(StepForward);
    }


    private void OneStepNormal(StepRecord stepInfo)
    {
        var frontSquareState = DecideFrontPositionType(stepInfo);
        var nearbyBeacons = GetNearbyBeacons();
        stepInfo.DecidedAction = "Undecided";

        if (Role == Role.Beacon)
        {
            if (nearbyBeacons.Length >= 3 && Tools.RandomFloat() > 0.7)
            {
                Role = Role.Walker;
                Cardinality = null;
            }
            else
            {
                stepInfo.DecidedAction = nameof(DoNothing);
                stepInfo.ActionSuccessful = DoNothing();
                return;
            }
        }
        else if (Role == Role.Walker && nearbyBeacons.Length < 2
            && (HoldingItem is null || Tools.RandomFloat() > 0.2)
            )
        {
            if (NearBase(stepInfo) is not null)
            {
                Role = Role.Beacon;
                Cardinality = 1;
                stepInfo.DecidedAction = nameof(DoNothing);
                stepInfo.ActionSuccessful = DoNothing();
                return;
            }
            else if (nearbyBeacons.Length > 0)
            {
                Role = Role.Beacon;
                Cardinality = nearbyBeacons.Single().Cardinality + 1;
                stepInfo.DecidedAction = nameof(DoNothing);
                stepInfo.ActionSuccessful = DoNothing();
                return;
            }
        }
        else
        {
            if (HoldingItem is not null && frontSquareState == TerrainType.Base)
            {
                stepInfo.DecidedAction = nameof(PutDownAnItem);
                stepInfo.ActionSuccessful = PutDownAnItem();
                return;
            }
            if (HoldingItem is null && frontSquareState == TerrainType.OccupiedByItem)
            {
                stepInfo.DecidedAction = nameof(GraspAnItem);
                stepInfo.ActionSuccessful = GraspAnItem();
                return;
            }
            if (HoldingItem is not null && !_avoidingObstacle)
            {
                var avoidCenter = false;
                var targetPosition = NearBase(stepInfo);
                if (targetPosition is null && nearbyBeacons.Length > 0)
                {
                    var bestBeacon = nearbyBeacons.MinBy(beacon => beacon.Cardinality);
                    targetPosition = (bestBeacon.__X, bestBeacon.__Y);
                    avoidCenter = true;
                }
                if (targetPosition != null)
                {
                    switch (StepApproachingPosition(targetPosition.Value, avoidCenter))
                    {
                        case nameof(StepForward):
                            if (frontSquareState == TerrainType.Free)
                            {
                                stepInfo.DecidedAction = nameof(StepForward);
                                stepInfo.ActionSuccessful = StepForward();
                                return;
                            }
                            if (frontSquareState == TerrainType.Obstacle
                                && Tools.RandomFloat() >= 0.95)
                            {
                                stepInfo.DecidedAction = nameof(Destroy);
                                stepInfo.ActionSuccessful = Destroy();
                                return;
                            }
                            break;

                        case nameof(TurnLeft):
                            stepInfo.DecidedAction = nameof(TurnLeft);
                            stepInfo.ActionSuccessful = TurnLeft();
                            break;

                        case nameof(TurnRight):
                            stepInfo.DecidedAction = nameof(TurnRight);
                            stepInfo.ActionSuccessful = TurnRight();
                            break;
                    }
                }
            }
        }

        switch (frontSquareState)
        {
            case TerrainType.Free:
                stepInfo.DecidedAction = nameof(StepForward);
                stepInfo.ActionSuccessful = StepForward();
                _avoidingObstacle = false;
                return;

            case TerrainType.OccupiedByItem:
                if (HoldingItem is null)
                {
                    stepInfo.DecidedAction = nameof(GraspAnItem);
                    stepInfo.ActionSuccessful = GraspAnItem();
                    return;
                }
                break;

            case TerrainType.Obstacle:
                if (Tools.RandomFloat() >= 0.95)
                {
                    stepInfo.DecidedAction = nameof(Destroy);
                    stepInfo.ActionSuccessful = Destroy();
                    return;
                }
                break;

            case TerrainType.Base:
                if (HoldingItem is not null)
                {
                    stepInfo.DecidedAction = nameof(PutDownAnItem);
                    stepInfo.ActionSuccessful = PutDownAnItem();
                    return;
                }
                break;
        }

        if (stepInfo.DecidedAction == "Undecided")
        {
            _avoidingObstacle = true;
            var leftPosition = GetLeftPosition(stepInfo);

            if (leftPosition is not null
            && leftPosition.Value.X >= 0
            && leftPosition.Value.X < _mapMemory.Length
            && leftPosition.Value.Y >= 0
            && leftPosition.Value.Y < _mapMemory[0].Length)
            {
                var leftPoint = _mapMemory[leftPosition.Value.X][leftPosition.Value.Y];
                if (leftPoint?.TerrainType == TerrainType.Free
                    || (HoldingItem is null && leftPoint?.TerrainType == TerrainType.OccupiedByItem)
                    || (HoldingItem is not null && leftPoint?.TerrainType == TerrainType.Base))
                {
                    stepInfo.DecidedAction = nameof(TurnLeft);
                    stepInfo.ActionSuccessful = TurnLeft();
                }
                else
                {
                    stepInfo.DecidedAction = nameof(TurnRight);
                    stepInfo.ActionSuccessful = TurnRight();
                }
            }
            else
            {
                stepInfo.DecidedAction = nameof(TurnRight);
                stepInfo.ActionSuccessful = TurnRight();
            }
        }
    }

    private void OneStepArsonist(StepRecord stepInfo)
    {
        var frontSquareState = DecideFrontPositionType(stepInfo);
        var nearbyBeacons = GetNearbyBeacons();
        stepInfo.DecidedAction = "Undecided";

        if (nearbyBeacons.Length > 0 && !_avoidingObstacle)
        {
            var nearBeacon = nearbyBeacons.First();
            var targetPosition = (nearBeacon.__X, nearBeacon.__Y);
            switch (StepApproachingPosition(targetPosition, false))
            {
                case nameof(StepForward):
                    if (frontSquareState == TerrainType.Free)
                    {
                        stepInfo.DecidedAction = nameof(StepForward);
                        stepInfo.ActionSuccessful = StepForward();
                        return;
                    }
                    break;

                case nameof(TurnLeft):
                    stepInfo.DecidedAction = nameof(TurnLeft);
                    stepInfo.ActionSuccessful = TurnLeft();
                    break;

                case nameof(TurnRight):
                    stepInfo.DecidedAction = nameof(TurnRight);
                    stepInfo.ActionSuccessful = TurnRight();
                    break;
            }
        }

        switch (frontSquareState)
        {
            case TerrainType.Free:
                stepInfo.DecidedAction = nameof(StepForward);
                stepInfo.ActionSuccessful = StepForward();
                _avoidingObstacle = false;
                break;

            case TerrainType.OccupiedByItem:
            case TerrainType.Obstacle:
            case TerrainType.OccupiedByRobot:
                if (Tools.RandomFloat() >= 0.95)
                {
                    stepInfo.DecidedAction = nameof(StartFire);
                    stepInfo.ActionSuccessful = StartFire();
                }
                break;
        }

        if (stepInfo.DecidedAction == "Undecided")
        {
            var leftPosition = GetLeftPosition(stepInfo);
            _avoidingObstacle = true;

            if (leftPosition is not null
            && leftPosition.Value.X >= 0
            && leftPosition.Value.X < _mapMemory.Length
            && leftPosition.Value.Y >= 0
            && leftPosition.Value.Y < _mapMemory[0].Length)
            {
                var leftPoint = _mapMemory[leftPosition.Value.X][leftPosition.Value.Y];
                if (leftPoint?.TerrainType == TerrainType.Free
                    || leftPoint?.TerrainType == TerrainType.OccupiedByItem)
                {
                    stepInfo.DecidedAction = nameof(TurnLeft);
                    stepInfo.ActionSuccessful = TurnLeft();
                }
                else
                {
                    stepInfo.DecidedAction = nameof(TurnRight);
                    stepInfo.ActionSuccessful = TurnRight();
                }
            }
            else
            {
                stepInfo.DecidedAction = nameof(TurnRight);
                stepInfo.ActionSuccessful = TurnRight();
            }
        }
    }


    private void OneStepItemDestroyer(StepRecord stepInfo)
    {
        var frontSquareState = DecideFrontPositionType(stepInfo);
        var nearbyBeacons = GetNearbyBeacons();
        stepInfo.DecidedAction = "Undecided";

        switch (frontSquareState)
        {
            case TerrainType.Free:
                stepInfo.DecidedAction = nameof(StepForward);
                stepInfo.ActionSuccessful = StepForward();
                _avoidingObstacle = false;
                break;

            case TerrainType.OccupiedByItem:
                if (Tools.RandomFloat() >= 0.30)
                {
                    stepInfo.DecidedAction = nameof(Destroy);
                    stepInfo.ActionSuccessful = Destroy();
                }
                break;
        }

        if (stepInfo.DecidedAction == "Undecided")
        {
            var leftPosition = GetLeftPosition(stepInfo);
            _avoidingObstacle = true;

            if (leftPosition is not null
            && leftPosition.Value.X >= 0
            && leftPosition.Value.X < _mapMemory.Length
            && leftPosition.Value.Y >= 0
            && leftPosition.Value.Y < _mapMemory[0].Length)
            {
                var leftPoint = _mapMemory[leftPosition.Value.X][leftPosition.Value.Y];
                if (leftPoint?.TerrainType == TerrainType.Free
                    || leftPoint?.TerrainType == TerrainType.OccupiedByItem)
                {
                    stepInfo.DecidedAction = nameof(TurnLeft);
                    stepInfo.ActionSuccessful = TurnLeft();
                }
                else
                {
                    stepInfo.DecidedAction = nameof(TurnRight);
                    stepInfo.ActionSuccessful = TurnRight();
                }
            }
            else
            {
                stepInfo.DecidedAction = nameof(TurnRight);
                stepInfo.ActionSuccessful = TurnRight();
            }
        }
    }


    private void OneStepLiar(StepRecord stepInfo)
    {
        var frontSquareState = DecideFrontPositionType(stepInfo);
        var nearbyBeacons = GetNearbyBeacons();
        stepInfo.DecidedAction = "Undecided";

        if (Role == Role.Beacon)
        {
            if (Tools.RandomFloat() > 0.95)
            {
                Role = Role.Walker;
                Cardinality = null;
            }
            else
            {
                stepInfo.DecidedAction = nameof(DoNothing);
                stepInfo.ActionSuccessful = DoNothing();
                return;
            }
        }
        else if (Role == Role.Walker && nearbyBeacons.Length > 1
            && nearbyBeacons.Length < 3)
        {
            Role = Role.Beacon;
            Cardinality = int.Max(nearbyBeacons.Min(beacon => beacon.Cardinality) - 1, 1);
            stepInfo.DecidedAction = nameof(DoNothing);
            stepInfo.ActionSuccessful = DoNothing();
            return;
        }

        switch (frontSquareState)
        {
            case TerrainType.Free:
                stepInfo.DecidedAction = nameof(StepForward);
                stepInfo.ActionSuccessful = StepForward();
                _avoidingObstacle = false;
                return;
        }

        if (stepInfo.DecidedAction == "Undecided")
        {
            _avoidingObstacle = true;
            var leftPosition = GetLeftPosition(stepInfo);

            if (leftPosition is not null
            && leftPosition.Value.X >= 0
            && leftPosition.Value.X < _mapMemory.Length
            && leftPosition.Value.Y >= 0
            && leftPosition.Value.Y < _mapMemory[0].Length)
            {
                var leftPoint = _mapMemory[leftPosition.Value.X][leftPosition.Value.Y];
                if (leftPoint?.TerrainType == TerrainType.Free
                    || (HoldingItem is null && leftPoint?.TerrainType == TerrainType.OccupiedByItem)
                    || (HoldingItem is not null && leftPoint?.TerrainType == TerrainType.Base))
                {
                    stepInfo.DecidedAction = nameof(TurnLeft);
                    stepInfo.ActionSuccessful = TurnLeft();
                }
                else
                {
                    stepInfo.DecidedAction = nameof(TurnRight);
                    stepInfo.ActionSuccessful = TurnRight();
                }
            }
            else
            {
                stepInfo.DecidedAction = nameof(TurnRight);
                stepInfo.ActionSuccessful = TurnRight();
            }
        }
    }

    public void OneStep(uint time)
    {
        if (!Alive)
        {
            return;
        }

        var stepInfo = new StepRecord
        {
            StartTime = time,
            __RealTemperature = __Map.GetTemperature(__Position),
            __RealOrientation = __Orientation,
            __RealGpsX = __Position.X,
            __RealGpsY = __Position.Y,
            RobotId = Id,
            FromPositionId = __Map.GetPositionId(__Position),
        };
        ReadSensors(stepInfo);

        switch (Profile)
        {
            case RobotProfile.Normal:
            case RobotProfile.Broken:
                OneStepNormal(stepInfo);
                break;

            case RobotProfile.Arsonist:
                OneStepArsonist(stepInfo);
                break;

            case RobotProfile.Liar:
                OneStepLiar(stepInfo);
                break;

            case RobotProfile.ItemDestroyer:
                OneStepItemDestroyer(stepInfo);
                break;
        }

        stepInfo.Cardinality = Cardinality;
        stepInfo.ToPositionId = __Map.GetPositionId(__Position);

        stepInfo.Log();
        foreach (var memoryMapCol in _mapMemory)
        {
            foreach (var memoryPoint in memoryMapCol)
            {
                memoryPoint?.Log(time);
            }
        }
    }

    public void Log()
    {
        File.AppendAllText("dym_robot.tsv", $"{Id}\t{Profile}\t{_gps.DamageLevel}\t{_imu.DamageLevel}\t{_lidar.DamageLevel}\t{_radar.DamageLevel}\t{_thermometer.DamageLevel}\n");
    }
}
