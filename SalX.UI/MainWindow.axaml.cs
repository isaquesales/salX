using Avalonia.Controls;
using Avalonia.Input;
using SalX;
using SalX.Numbers;
using System;
namespace SalX.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SolveButton.Click += (_, _) => Solve();

        InputBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Solve();
                e.Handled = true;
            }
        };
    }

    private void Solve()
    {
        try
        {
            var expr = InputBox.Text;
            if (string.IsNullOrWhiteSpace(expr))
                return;

            var number = Number.Parse(expr);

            OutputBox.Text = OnlyFinalCheck.IsChecked == true
                ? Engine.DoString(number)
                : string.Join(Environment.NewLine, Engine.CollectSteps(number));
        }
        catch (Exception ex)
        {
            OutputBox.Text = "Error: " + ex.Message;
        }
    }
}
