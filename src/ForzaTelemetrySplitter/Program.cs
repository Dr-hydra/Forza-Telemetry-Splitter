using System.Globalization;
using System.Threading;
using ForzaTelemetrySplitter.Config;
using ForzaTelemetrySplitter.Core;
using ForzaTelemetrySplitter.UI;

namespace ForzaTelemetrySplitter;

internal static class Program
{
    // Ensure only one instance runs — two splitters would fight over the same listen port.
    private static Mutex? _singleInstance;

    [STAThread]
    private static void Main()
    {
        _singleInstance = new Mutex(initiallyOwned: true, "ForzaTelemetrySplitter.SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show(
                "Forza Telemetry Splitter is already running.\n\nLook for its icon in the system tray (bottom-right of the taskbar).",
                "Already running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var config = ConfigStore.Load();

        // Apply the UI language BEFORE any form is built so every control picks up the right strings.
        // Auto follows the Windows display language; unsupported languages fall back to English.
        ApplyCulture(config.Language);

        var engine = new SplitterEngine();
        var overlay = new OverlayForm();
        var mainForm = new MainForm(config, engine, overlay);
        var tray = new TrayContext(config, mainForm, overlay);

        // On the very first launch, walk the user through pointing Forza at the splitter.
        bool showedWelcome = false;
        if (!config.FirstRunComplete)
        {
            using var welcome = new WelcomeForm(config);
            welcome.ShowDialog();
            config.FirstRunComplete = welcome.DontShowAgain;
            ConfigStore.Save(config);
            showedWelcome = true;
        }

        // Start splitting immediately if configured to (default), so it works the moment Forza sends.
        if (config.AutoStartSplitting)
            mainForm.ToggleRunning();

        // After the first-run welcome, open the main window so the user sees the app is running.
        // (Normal launches start in the tray.)
        if (showedWelcome)
        {
            mainForm.Show();
            mainForm.Activate();
        }

        // Run headless in the tray. The main window is shown on demand from the tray menu.
        Application.Run(tray);

        _singleInstance.ReleaseMutex();
    }

    /// <summary>Set the process UI culture (and default for new threads) from the language setting.</summary>
    internal static void ApplyCulture(AppLanguage language)
    {
        var culture = language.ToCulture();
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
