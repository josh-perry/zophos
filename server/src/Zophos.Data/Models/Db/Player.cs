namespace Zophos.Data.Models.Db;

public class Player
{
    public Guid Id { get; set; }

    public string Name { get; set; }
    
    public float X { get; set; }
    
    public float Y { get; set; }
}