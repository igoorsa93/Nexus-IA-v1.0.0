using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace NexusIA;

public partial class App : Application
{
    // Uma única instância: abrir o NEXUS IA de novo só traz a janela que já está aberta para a frente.
    private static Mutex? _single;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string? cls, string title);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);

    public App()
    {
        // Captura exceções não tratadas em qualquer thread da UI
        DispatcherUnhandledException += (s, ev) =>
        {
            ev.Handled = true;
            try
            {
                System.Windows.MessageBox.Show(
                    $"O NEXUS IA encontrou um erro e será fechado:\n\n{ev.Exception.Message}\n\n{ev.Exception.StackTrace}",
                    "NEXUS IA - Erro",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            catch { }
            Environment.Exit(1);
        };
        // Captura exceções de threads não-UI
        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            try
            {
                System.Windows.MessageBox.Show(
                    $"Erro crítico no NEXUS IA:\n\n{ev.ExceptionObject}",
                    "NEXUS IA - Erro Crítico",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            catch { }
            Environment.Exit(1);
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        bool first;
        try { _single = new Mutex(true, @"Local\NexusIA.SingleInstance", out first); }
        catch (UnauthorizedAccessException) { first = false; } // criado por outra instância com outro nível de permissão

        if (!first)
        {
            IntPtr h = FindWindow(null, "NEXUS IA");
            if (h != IntPtr.Zero)
            {
                if (IsIconic(h)) ShowWindow(h, 9); // SW_RESTORE
                SetForegroundWindow(h);
            }
            Shutdown();
            return;
        }
        base.OnStartup(e);
        try
        {
            new MainWindow().Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Falha ao iniciar a janela principal:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "NEXUS IA - Erro de Inicialização",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown();
        }
    }
}
