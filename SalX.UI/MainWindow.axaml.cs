using Avalonia.Controls;
using Avalonia.Threading;
using SalX;
using SalX.Numbers;
using System;
namespace SalX.UI.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _debounce;

    public MainWindow()
    {
        InitializeComponent();

        _debounce = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Solve();
        };

        InputBox.TextChanged += (_, _) => RestartDebounce();
        OnlyFinalCheck.IsCheckedChanged += (_, _) => RestartDebounce();

        RestartDebounce();
    }

    private void RestartDebounce()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Solve()
    {
        try
        {
            var expr = InputBox.Text;

            if (string.IsNullOrWhiteSpace(expr))
            {
                OutputBox.Text = "";
                return;
            }

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
