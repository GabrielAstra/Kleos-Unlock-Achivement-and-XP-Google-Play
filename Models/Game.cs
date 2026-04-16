namespace Kleos.Models;

public class Game
{
    public int Id { get; set; }
    public string ExternalGameId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string DeveloperName { get; set; } = "";
    public int AchievementTotalCount { get; set; }

    public override string ToString() => $"{DisplayName} ({DeveloperName})";
}
