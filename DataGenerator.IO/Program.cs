using System.CommandLine;

using DataGenerator;
using DataGenerator.Enums;
using DataGenerator.Environment;

static void RunSimulation(bool printHeader, int seed, bool printMap, int speed,
    int normalCount, int liarCount, int brokenCount, int destroyerCount,
    int arsonistCount, uint mapSizeX, uint mapSizeY, double itemProbability,
    double obstacleCoverage, uint stepsCount)
{
    if (printHeader)
    {
        File.WriteAllText("dym_item.tsv", "id\n");
        File.WriteAllText("dym_robot.tsv", "id\tprofile\tgps_damage_level\timu_damage_level\tlidar_damage_level\tradar_damage_level\tthermometer_damage_level\n");
        File.WriteAllText("fact_step.tsv", "start_time\trobot_id\tfrom_position_id\tto_position_id\tmeasured_gps_x\tmeasured_gps_y\treal_gps_x\treal_gps_y\tmeasured_orientation\treal_orientation\tmeasured_lidar_distance\tmeasured_radar_distance\treal_distance\tmeasured_temperature\treal_temperature\tdecided_action\taction_successful\tholding_item_id\tcardinality\n");
        File.WriteAllText("fact_memory_map_point.tsv", "time\tx\ty\trobot_id\tterrain_type\toccupied_by_item_id\toccupied_by_robot_id\n");
        File.WriteAllText("dym_map_point.tsv", "id\tx\ty\ttemperature\n");
        File.WriteAllText("fact_map_point_state.tsv", "time\tmap_point_id\tterrain_type\toccupied_by_item_id\toccupied_by_robot_id\n");
    }
    else
    {
        File.Create("dym_item.tsv").Dispose();
        File.Create("dym_robot.tsv").Dispose();
        File.Create("fact_step.tsv").Dispose();
        File.Create("fact_memory_map_point.tsv").Dispose();
        File.Create("dym_map_point.tsv").Dispose();
        File.Create("fact_map_point_state.tsv").Dispose();
    }

    Tools.SetSeed(seed);
    var map = Map.GenerateRandom(mapSizeX, mapSizeY, seed, itemProbability,
        obstacleCoverage);

    if (printMap)
    {
        Console.Clear();
        map.Print();
    }

    var robots = Enumerable.Repeat(RobotProfile.Normal, normalCount)
        .Concat(Enumerable.Repeat(RobotProfile.Broken, brokenCount))
        .Concat(Enumerable.Repeat(RobotProfile.ItemDestroyer, destroyerCount))
        .Concat(Enumerable.Repeat(RobotProfile.Liar, liarCount))
        .Concat(Enumerable.Repeat(RobotProfile.Arsonist, arsonistCount))
        .Select(profile => new Robot(map, mapSizeX, mapSizeY, profile))
        .ToArray();

    map.PlaceRobots(robots, seed);

    map.LogDym();
    foreach (var robot in robots)
    {
        robot.Log();
    }

    if (printMap)
    {
        Console.Clear();
        map.Print();
    }

    var random = new Random(seed);

    for (uint i = 0; i < stepsCount; i++)
    {
        random.Shuffle(robots);
        foreach (var robot in robots)
        {
            robot.OneStep(i);
        }
        if (printMap)
        {
            Console.Clear();
            map.Print();
            Thread.Sleep(1000 / speed);
        }
        map.LogFact(i);
    }
}

var headerOption = new Option<bool>(
    name: "--headers",
    description: "Whether a header should be printed to start of each output file.",
    getDefaultValue: () => false
);

var seedOption = new Option<int>(
    name: "--seed",
    description: "Seed of the random generator.",
    getDefaultValue: () => 13
);
seedOption.AddAlias("-s");

var animationOption = new Option<bool>(
    name: "--animation",
    description: "Whether the program should show live animation of the simulation.",
    getDefaultValue: () => false
);

var speedOption = new Option<int>(
    name: "--speed",
    description: "Speed of the animation (use range 1 - 5).",
    getDefaultValue: () => 2
);

var normalOption = new Option<int>(
    name: "--normal",
    description: "Count of the robots with normal behavior.",
    getDefaultValue: () => 40
);
normalOption.AddAlias("-n");

var brokenOption = new Option<int>(
    name: "--broken",
    description: "Count of the robots with one random broken sensor.",
    getDefaultValue: () => 2
);
brokenOption.AddAlias("-b");

var arsonistOption = new Option<int>(
    name: "--arsonist",
    description: "Count of the robots with arsonist behavior.",
    getDefaultValue: () => 2
);
arsonistOption.AddAlias("-a");

var destroyerOption = new Option<int>(
    name: "--destroyer",
    description: "Count of the robots which destroy collected items.",
    getDefaultValue: () => 2
);
destroyerOption.AddAlias("-d");

var liarOption = new Option<int>(
    name: "--liar",
    description: "Count of the robots which lie to others.",
    getDefaultValue: () => 2
);
liarOption.AddAlias("-l");

var mapSizeXOption = new Option<uint>(
    name: "--xsize",
    description: "Size of the map X axis.",
    getDefaultValue: () => 40
);
mapSizeXOption.AddAlias("-x");

var mapSizeYOption = new Option<uint>(
    name: "--ysize",
    description: "Size of the map Y axis.",
    getDefaultValue: () => 40
);
mapSizeYOption.AddAlias("-y");

var itemProbabilityOption = new Option<double>(
    name: "--itemprob",
    description: "Probability of item occurence on a square of the map (range 0 - 1).",
    getDefaultValue: () => 0.05
);
itemProbabilityOption.AddAlias("-i");

var obstacleCoverageOption = new Option<double>(
    name: "--obstcover",
    description: "Coverage of the map by obstacles (range 0 - 1).",
    getDefaultValue: () => 0.2
);
obstacleCoverageOption.AddAlias("-o");

var stepsOption = new Option<uint>(
    name: "--steps",
    description: "Count of steps to simulate, i.e., length of the simulation.",
    getDefaultValue: () => 300
);
stepsOption.AddAlias("-t");

var rootCommand = new RootCommand("Application for simulating a multi-robot system inside a burning house.");
rootCommand.AddOption(headerOption);
rootCommand.AddOption(seedOption);
rootCommand.AddOption(animationOption);
rootCommand.AddOption(speedOption);
rootCommand.AddOption(normalOption);
rootCommand.AddOption(liarOption);
rootCommand.AddOption(brokenOption);
rootCommand.AddOption(destroyerOption);
rootCommand.AddOption(arsonistOption);
rootCommand.AddOption(mapSizeXOption);
rootCommand.AddOption(mapSizeYOption);
rootCommand.AddOption(itemProbabilityOption);
rootCommand.AddOption(obstacleCoverageOption);
rootCommand.AddOption(stepsOption);

rootCommand.SetHandler((context) => {
        bool headers = context.ParseResult.GetValueForOption(headerOption);
        int seed = context.ParseResult.GetValueForOption(seedOption);
        bool animation = context.ParseResult.GetValueForOption(animationOption);
        var speed = context.ParseResult.GetValueForOption(speedOption);
        var normal = context.ParseResult.GetValueForOption(normalOption);
        var liar = context.ParseResult.GetValueForOption(liarOption);
        var broken = context.ParseResult.GetValueForOption(brokenOption);
        var destroyer = context.ParseResult.GetValueForOption(destroyerOption);
        var arsonist = context.ParseResult.GetValueForOption(arsonistOption);
        var x = context.ParseResult.GetValueForOption(mapSizeXOption);
        var y = context.ParseResult.GetValueForOption(mapSizeYOption);
        var itemprob = context.ParseResult.GetValueForOption(itemProbabilityOption);
        var obstcov = context.ParseResult.GetValueForOption(obstacleCoverageOption);
        var steps = context.ParseResult.GetValueForOption(stepsOption);
        RunSimulation(headers, seed, animation, speed, normal, liar, broken,
            destroyer, arsonist, x, y, itemprob, obstcov, steps);
    });

return rootCommand.Invoke(args);
