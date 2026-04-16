using System.Windows;

namespace Kleos.Views;

public partial class IncrementalDialog : Window
{
    public int Steps { get; private set; }

    public IncrementalDialog(string achievementName, int totalSteps)
    {
        InitializeComponent();
        TxtLabel.Text = $"{achievementName}\nTotal steps: {totalSteps}. How many steps to increment?";
        TxtSteps.Text = totalSteps.ToString();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtSteps.Text, out int val) && val >= 0)
        {
            Steps = val;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Enter a valid number >= 0.");
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
