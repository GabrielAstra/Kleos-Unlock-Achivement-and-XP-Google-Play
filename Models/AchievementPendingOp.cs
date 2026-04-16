namespace Kleos.Models;

public class AchievementPendingOp
{
    public int Id { get; set; }
    public int ClientContextId { get; set; }
    public string ExternalAchievementId { get; set; } = "";
    public int AchievementType { get; set; }
    public int NewState { get; set; }
    public string StepsToIncrement { get; set; } = "";
    public string MinStepsToSet { get; set; } = "";
    public string ExternalGameId { get; set; } = "";
    public string ExternalPlayerId { get; set; } = "";
}
