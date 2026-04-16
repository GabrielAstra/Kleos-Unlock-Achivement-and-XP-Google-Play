namespace Kleos.Models;

public class AchievementInstance
{
    public int Id { get; set; }
    public int DefinitionId { get; set; }
    public int PlayerId { get; set; }
    public int State { get; set; }
    public int CurrentSteps { get; set; }
    public string FormattedCurrentSteps { get; set; } = "";
    public long LastUpdatedTimestamp { get; set; }
    public int InstanceXpValue { get; set; }

    public bool IsUnlocked => State == 0;
    public bool IsLocked => State > 0;
}
