using System.IO;
using System.Windows;
using Microsoft.Win32;
using Kleos.ViewModels;

namespace Kleos.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private string _adbPath = "";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        _adbPath = FindAdb();

        var dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbs");
        if (Directory.Exists(dbDir))
        {
            var files = Directory.GetFiles(dbDir, "games_*.db");
            if (files.Length > 0)
                _vm.LoadDatabase(files[0]);
        }
    }

    private string FindAdb()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(localAppData, "Android", "Sdk", "platform-tools", "adb.exe");
        return File.Exists(path) ? path : "adb";
    }

    private async void BtnPullFromDevice_Click(object sender, RoutedEventArgs e)
    {
        _vm.StatusMessage = "Pulling database from device...";

        var (localPath, error) = await Task.Run(() =>
        {
            var findResult = RunAdb("shell su -c 'ls /data/data/com.google.android.gms/databases/games_*.db'");
            var remotePath = findResult.Trim();

            if (string.IsNullOrEmpty(remotePath) || remotePath.Contains("No such file") || remotePath.Contains("not found"))
                return ((string?)null, $"Error: {(string.IsNullOrEmpty(remotePath) ? "adb not found or no device connected" : remotePath)}");

            var dbFileName = remotePath.Split('/').Last();
            var localDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbs");
            Directory.CreateDirectory(localDir);
            var localPath = Path.Combine(localDir, dbFileName);

            RunAdb($"shell su -c 'cp {remotePath} /sdcard/{dbFileName} && cp {remotePath}-wal /sdcard/{dbFileName}-wal 2>/dev/null; cp {remotePath}-shm /sdcard/{dbFileName}-shm 2>/dev/null'");
            var pullResult = RunAdb($"pull /sdcard/{dbFileName} \"{localPath}\"");
            RunAdb($"pull /sdcard/{dbFileName}-wal \"{localPath}-wal\"");
            RunAdb($"pull /sdcard/{dbFileName}-shm \"{localPath}-shm\"");
            RunAdb($"shell rm /sdcard/{dbFileName} /sdcard/{dbFileName}-wal /sdcard/{dbFileName}-shm 2>/dev/null");

            if (!File.Exists(localPath))
                return ((string?)null, $"Failed to pull database. adb output: {pullResult}");

            return (localPath, (string?)null);
        });

        if (error != null) { _vm.StatusMessage = error; return; }

        _vm.LoadDatabase(localPath!);
        _vm.StatusMessage = $"Pulled and loaded {Path.GetFileName(localPath)} from device.";
    }

    private void BtnOpenDb_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "SQLite Database|*.db|All Files|*.*", Title = "Select games_*.db file" };
        if (dlg.ShowDialog() == true)
            _vm.LoadDatabase(dlg.FileName);
    }

    private void BtnReload_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_vm.DbPath))
            _vm.LoadDatabase(_vm.DbPath);
    }

    private void BtnUnlockSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedAchievement == null) { _vm.StatusMessage = "No achievement selected."; return; }

        var ach = _vm.SelectedAchievement;
        if (ach.IsIncremental)
        {
            var dlg = new IncrementalDialog(ach.Name, ach.TotalSteps);
            if (dlg.ShowDialog() == true)
                _vm.UnlockAchievement(ach, dlg.Steps.ToString());
        }
        else
        {
            _vm.UnlockAchievement(ach);
        }
    }

    private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedGame == null) { _vm.StatusMessage = "Select a game first."; return; }
        var result = MessageBox.Show($"Unlock ALL achievements for {_vm.SelectedGame.DisplayName}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
            _vm.UnlockAll();
    }

    private void BtnUnlockById_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new UnlockByIdDialog();
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.AchievementId))
        {
            var ach = _vm.Achievements.FirstOrDefault(a => a.ExternalAchievementId == dlg.AchievementId);
            if (ach == null) { _vm.StatusMessage = $"Achievement ID not found: {dlg.AchievementId}"; return; }
            _vm.UnlockAchievement(ach);
        }
    }

    private void BtnRemoveDuplicates_Click(object sender, RoutedEventArgs e) =>
        _vm.RemoveDuplicatePendingOps();

    private void BtnClearOps_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Clear all pending ops?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
            _vm.ClearPendingOps();
    }

    private async void BtnPushDb_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.DbPath)) { _vm.StatusMessage = "No database loaded."; return; }

        var dbPath = _vm.DbPath;
        _vm.StatusMessage = "Flushing database...";

        _vm.FlushAndClose();

        _vm.StatusMessage = "Pushing database to device...";

        var error = await Task.Run(() =>
        {
            var dbFileName = Path.GetFileName(dbPath);
            var sdcardPath = $"/sdcard/{dbFileName}";
            var remotePath = $"/data/data/com.google.android.gms/databases/{dbFileName}";

            var pushResult = RunAdb($"push \"{dbPath}\" {sdcardPath}");
            if (pushResult.Contains("error") || pushResult.Contains("failed"))
                return $"Push failed: {pushResult}";

            var cpResult = RunAdb($"shell su -c 'cp {sdcardPath} {remotePath} && chmod 660 {remotePath} && rm {sdcardPath}'");

            RunAdb($"shell su -c 'rm -f {remotePath}-wal {remotePath}-shm'");

            RunAdb("shell su -c 'am force-stop com.google.android.gms'");
            RunAdb("shell su -c 'am startservice -n com.google.android.gms/.chimera.PersistentDirectBootAwareService'");

            return (string?)null;
        });

        if (error != null)
        {
            _vm.StatusMessage = error;
            _vm.LoadDatabase(dbPath);
            return;
        }

        _vm.StatusMessage = $"Pushed {Path.GetFileName(dbPath)} to device successfully.";

        _vm.LoadDatabase(dbPath);
    }

    private string RunAdb(string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _adbPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var proc = System.Diagnostics.Process.Start(psi);
        proc?.WaitForExit();
        return (proc?.StandardOutput.ReadToEnd() + proc?.StandardError.ReadToEnd())?.Trim() ?? "";
    }
}
