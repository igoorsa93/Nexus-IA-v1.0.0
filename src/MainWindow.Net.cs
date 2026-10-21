using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NexusIA;

/// <summary>Páginas Energia (bateria e fonte) e Rede (diagnóstico de lag).</summary>
public partial class MainWindow
{
    // ================= Energia =================

    private bool _energyBusy;

    private async void RefreshEnergy()
    {
        if (_energyBusy) return;
        _energyBusy = true;
        BtnEnergyRefresh.IsEnabled = false;
        EnergyStatus.Text = "Lendo bateria e energia…";
        try
        {
            var sensors = _monitor.Rows.ToList();
            var cards = await Task.Run(() => BatteryInfo.Read(sensors));
            EnergyCards.ItemsSource = cards;
            bool hasBattery = cards.Count > 1;
            EnergyStatus.Text = (hasBattery ? "Bateria detectada" : "Nenhuma bateria (PC de mesa)") + $" · lido às {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex) { EnergyStatus.Text = "Falhou: " + ex.Message; }
        finally
        {
            _energyBusy = false;
            BtnEnergyRefresh.IsEnabled = true;
        }
    }

    private void BtnEnergyRefresh_Click(object sender, RoutedEventArgs e) => RefreshEnergy();

    // ================= Rede =================

    private CancellationTokenSource? _netCts;
    private bool _netRunning;
    private List<DiskHealthCard>? _lastNet;

    private async void BtnNetTest_Click(object sender, RoutedEventArgs e)
    {
        if (_netRunning) { _netCts?.Cancel(); return; }
        if (AnyBenchRunning || TestRunning) { NetStatus.Text = "Termine o outro teste antes."; return; }

        _netRunning = true;
        _netCts = new CancellationTokenSource();
        var ct = _netCts.Token;
        BtnNetTest.Content = "CANCELAR";
        NetCards.ItemsSource = null;
        NetProgress.ScaleX = 0;
        try
        {
            var cards = await NetworkTest.Run((text, frac) => Dispatcher.BeginInvoke(new Action(() =>
            {
                NetStatus.Text = text;
                NetProgress.ScaleX = Math.Clamp(frac, 0, 1);
            })), ct);
            _lastNet = cards;
            NetCards.ItemsSource = cards;
            NetStatus.Text = $"Concluído às {DateTime.Now:HH:mm:ss}. O resultado entra no relatório exportado.";
            NetProgress.ScaleX = 1;
        }
        catch (OperationCanceledException) { NetStatus.Text = "Teste cancelado."; NetProgress.ScaleX = 0; }
        catch (Exception ex) { NetStatus.Text = "Falhou: " + ex.Message; NetProgress.ScaleX = 0; }
        finally
        {
            _netRunning = false;
            _netCts?.Dispose();
            _netCts = null;
            BtnNetTest.Content = "TESTAR";
        }
    }

    // ================= Aparência =================

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is not { } menu) return;
        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }

    private void Palette_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string palette }) return;
        _pendingPalette = palette;
        _settings.ColorPalette = _pendingPalette;
        ApplyAppearance();
    }

    private void ThemeMode_Click(object sender, RoutedEventArgs e)
    {
        _pendingLightTheme = !ThemeManager.IsLight;
        _settings.LightTheme = _pendingLightTheme;
        ApplyAppearance();
    }

    private void AppearanceAccent_Checked(object sender, RoutedEventArgs e)
    {
        if (!_appearanceSyncing && sender is RadioButton { Tag: string palette })
            _pendingPalette = palette;
    }

    private void AppearanceTheme_Checked(object sender, RoutedEventArgs e)
    {
        if (!_appearanceSyncing && sender is RadioButton { Tag: string mode })
            _pendingLightTheme = mode == "light";
    }

    private void AppearanceApply_Click(object sender, RoutedEventArgs e)
    {
        _settings.LightTheme = _pendingLightTheme;
        _settings.ColorPalette = _pendingPalette;
        ApplyAppearance();
    }

    private void AppearanceReset_Click(object sender, RoutedEventArgs e)
    {
        _pendingLightTheme = false;
        _pendingPalette = "green";
        _settings.LightTheme = _pendingLightTheme;
        _settings.ColorPalette = _pendingPalette;
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        ThemeManager.Apply(_settings.LightTheme, _settings.ColorPalette);
        _settings.ColorPalette = ThemeManager.CurrentPalette;
        _pendingLightTheme = _settings.LightTheme;
        _pendingPalette = _settings.ColorPalette;
        UpdateThemeButton();
        try { _settings.Save(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"A aparência foi aplicada, mas não pôde ser salva:\n\n{ex.Message}",
                "Nexus IA", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateThemeButton()
    {
        bool light = ThemeManager.IsLight;
        BtnTheme.Content = "Cores";
        BtnTheme.ToolTip = $"{ThemeManager.CurrentPaletteName} · modo {(light ? "claro" : "escuro")}";
        UpdateAppearanceControls();

        if (BtnTheme.ContextMenu is not { } menu) return;
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            if (item.Tag is not string tag) continue;
            if (tag == "mode")
                item.Header = light ? "Ativar modo escuro" : "Ativar modo claro";
            else
                item.IsChecked = string.Equals(tag, ThemeManager.CurrentPalette, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void UpdateAppearanceControls()
    {
        if (AppearanceDark == null || AppearanceLight == null) return;

        _appearanceSyncing = true;
        AppearanceDark.IsChecked = !ThemeManager.IsLight;
        AppearanceLight.IsChecked = ThemeManager.IsLight;
        foreach (var radio in new[] { AppearanceDark, AppearanceLight })
            if (radio.IsChecked == true && radio.Tag is string mode)
                _pendingLightTheme = mode == "light";
        foreach (var radio in AppearanceAccentButtons())
            radio.IsChecked = string.Equals(radio.Tag as string, ThemeManager.CurrentPalette, StringComparison.OrdinalIgnoreCase);
        _pendingPalette = ThemeManager.CurrentPalette;
        if (DashAppearanceSummary != null)
            DashAppearanceSummary.Text = $"{(ThemeManager.IsLight ? "Claro" : "Escuro")} · {ThemeManager.CurrentPaletteName}";
        _appearanceSyncing = false;
    }

    private RadioButton[] AppearanceAccentButtons() =>
    [
        AppearanceGreen, AppearanceCyan, AppearanceBlue, AppearancePurple, AppearancePink, AppearanceOrange, AppearanceAmber
    ];
}
