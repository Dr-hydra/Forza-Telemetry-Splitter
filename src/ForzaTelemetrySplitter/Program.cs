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
        var engine = new SplitterEngine();
        var overlay = new OverlayForm();
        var mainForm = new MainForm(config, engine, overlay);
        var tray = new TrayContext(config, mainForm, overlay);

        // On the very first launch, walk the user through pointing Forza at the splitter.
        if (!config.FirstRunComplete)
        {
            using var welcome = new WelcomeForm(config);
            welcome.ShowDialog();
            config.FirstRunComplete = welcome.DontShowAgain;
            ConfigStore.Save(config);
        }

        // Start splitting immediately if configured to (default), so it works the moment Forza sends.
        if (config.AutoStartSplitting)
            mainForm.ToggleRunning();

        // Run headless in the tray. The main window is shown on demand from the tray menu.
        Application.Run(tray);

        _singleInstance.ReleaseMutex();
    }
}
