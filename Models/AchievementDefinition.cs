namespace Kleos.Models;

public class AchievementDefinition
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string ExternalAchievementId { get; set; } = "";
    public int Type { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int TotalSteps { get; set; }
    public int InitialState { get; set; }
    public int DefinitionXpValue { get; set; }
    public double RarityPercent { get; set; }

    public bool IsNormal => Type == 0;
    public bool IsIncremental => Type == 1;
    public bool IsSecret => InitialState == 2;

    public string TypeLabel
    {
        get
        {
            var t = IsIncremental ? "INC" : "NOR";
            return IsSecret ? $"[{t}|SEC]" : $"[{t}]";
        }
    }

    public override string ToString() =>
        $"{TypeLabel} {ExternalAchievementId} | {Name} | {Description} | {DefinitionXpValue}xp";
}
