using System.Windows;

namespace Kleos.Views;

public partial class UnlockByIdDialog : Window
{
    public string AchievementId { get; private set; } = "";

    public UnlockByIdDialog() => InitializeComponent();

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        AchievementId = TxtId.Text.Trim();
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
