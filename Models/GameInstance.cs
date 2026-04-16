namespace Kleos.Models;

public class GameInstance
{
    public int Id { get; set; }
    public int InstanceGameId { get; set; }
    public string PackageName { get; set; } = "";
    public int Installed { get; set; }
}
