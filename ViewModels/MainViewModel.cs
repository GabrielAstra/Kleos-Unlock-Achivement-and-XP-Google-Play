using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Kleos.Data;
using Kleos.Models;

namespace Kleos.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private DbFile? _db;
    private string _dbPath = "";
    private Game? _selectedGame;
    private string _searchText = "";
    private string _statusMessage = "";
    private AchievementDefinition? _selectedAchievement;

    public ObservableCollection<Game> Games { get; } = new();
    public ObservableCollection<AchievementDefinition> Achievements { get; } = new();
    public ObservableCollection<Player> Players { get; } = new();
    public ObservableCollection<AchievementPendingOp> PendingOps { get; } = new();

    public string DbPath
    {
        get => _dbPath;
        set { _dbPath = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); ApplySearch(); }
    }

    public Game? SelectedGame
    {
        get => _selectedGame;
        set { _selectedGame = value; OnPropertyChanged(); LoadAchievements(); }
    }

    public AchievementDefinition? SelectedAchievement
    {
        get => _selectedAchievement;
        set { _selectedAchievement = value; OnPropertyChanged(); }
    }

    public void LoadDatabase(string path)
    {
        try
        {
            _db?.Dispose();
            _db = new DbFile(path);
            DbPath = path;
            _db.EnsureClientContextsForAllGames();
            LoadGames();
            LoadPlayers();
            LoadPendingOps();
            StatusMessage = "Database loaded successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading database: {ex.Message}";
        }
    }

    private void LoadGames()
    {
        Games.Clear();
        if (_db == null) return;
        foreach (var g in _db.GetGames())
            Games.Add(g);
    }

    private void LoadPlayers()
    {
        Players.Clear();
        if (_db == null) return;
        foreach (var p in _db.GetPlayers())
            Players.Add(p);
    }

    public void LoadPendingOps()
    {
        PendingOps.Clear();
        if (_db == null) return;
        foreach (var op in _db.GetPendingOps())
            PendingOps.Add(op);
    }

    private List<AchievementDefinition> _allAchievements = new();

    private void LoadAchievements()
    {
        Achievements.Clear();
        _allAchievements.Clear();
        if (_db == null || _selectedGame == null) return;

        try
        {
            _allAchievements = _db.GetAchievementDefinitions(_selectedGame.Id);
            ApplySearch();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading achievements: {ex.Message}";
        }
    }

    private void ApplySearch()
    {
        Achievements.Clear();
        var filtered = string.IsNullOrWhiteSpace(_searchText)
            ? _allAchievements
            : _allAchievements.Where(a =>
                a.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                a.ExternalAchievementId.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

        foreach (var a in filtered)
            Achievements.Add(a);
    }

    public void UnlockAchievement(AchievementDefinition ach, string stepsToIncrement = "")
    {
        if (_db == null) return;

        var inst = _db.GetAchievementInstance(ach.Id);
        if (inst == null) { StatusMessage = $"No instance found for {ach.Name}"; return; }
        if (inst.IsUnlocked) { StatusMessage = $"{ach.Name} is already unlocked."; return; }

        var game = _selectedGame ?? Games.FirstOrDefault(g => g.Id == ach.GameId);
        if (game == null) { StatusMessage = "Game not found."; return; }

        var gameInst = _db.GetGameInstance(game.Id);
        string packageName;
        if (gameInst != null)
        {
            packageName = gameInst.PackageName;
        }
        else
        {
            var ctx2 = _db.GetClientContexts().FirstOrDefault(c =>
                c.PackageName != "com.google.android.play.games");
            packageName = ctx2?.PackageName ?? game.ExternalGameId;
        }

        var ctx = _db.GetClientContextByPackageName(packageName);
        if (ctx == null)
        {
            ctx = _db.GetClientContexts().FirstOrDefault(c =>
                c.PackageName != "com.google.android.play.games");
        }
        if (ctx == null) { StatusMessage = "Client context not found."; return; }

        var player = Players.FirstOrDefault();
        if (player == null) { StatusMessage = "No player found."; return; }

        _db.AddPendingOp(new AchievementPendingOp
        {
            Id = _db.GetNextPendingOpId(),
            ClientContextId = ctx.Id,
            ExternalAchievementId = ach.ExternalAchievementId,
            AchievementType = ach.Type,
            NewState = 0,
            StepsToIncrement = stepsToIncrement,
            MinStepsToSet = "",
            ExternalGameId = game.ExternalGameId,
            ExternalPlayerId = player.ExternalPlayerId
        });

        StatusMessage = $"Unlocked: {ach.Name}";
        LoadPendingOps();
    }

    public void UnlockAll()
    {
        if (_db == null || _selectedGame == null) return;
        int count = 0;
        foreach (var ach in _allAchievements)
        {
            var inst = _db.GetAchievementInstance(ach.Id);
            if (inst != null && inst.IsLocked)
            {
                UnlockAchievement(ach, ach.IsIncremental ? ach.TotalSteps.ToString() : "");
                count++;
            }
        }
        StatusMessage = $"Queued {count} achievements for unlock.";
    }

    public void FlushAndClose()
    {
        _db?.FlushAndClose();
        _db = null;
    }

    public void ClearPendingOps()
    {
        if (_db == null) return;
        _db.EmptyPendingOps();
        LoadPendingOps();
        StatusMessage = "All pending ops cleared.";
    }

    public void RemoveDuplicatePendingOps()
    {
        if (_db == null) return;
        int removed = _db.RemoveDuplicatePendingOps();
        LoadPendingOps();
        StatusMessage = $"Removed {removed} duplicate pending ops.";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
