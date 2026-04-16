namespace Kleos.Models;

public class Player
{
    public int Id { get; set; }
    public string ExternalPlayerId { get; set; } = "";
    public string ProfileName { get; set; } = "";
    public int CurrentLevel { get; set; }
    public int CurrentXpTotal { get; set; }
    public int TotalUnlockedAchievements { get; set; }

    public override string ToString() => $"{ProfileName} (Level {CurrentLevel})";
}
