namespace Kleos.Models;

public class ClientContext
{
    public int Id { get; set; }
    public string PackageName { get; set; } = "";
    public int PackageUid { get; set; }
    public string AccountName { get; set; } = "";
    public string AccountType { get; set; } = "";
    public int IsGamesLite { get; set; }
}
