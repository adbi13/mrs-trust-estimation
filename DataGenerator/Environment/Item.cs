namespace DataGenerator.Environment;

public class Item : IBody
{
    private static int _idInit = 0;

    public int Id { get; set; } = _idInit++;
}
