using ForzaTelemetrySplitter.Config;
using ForzaTelemetrySplitter.Resources;

namespace ForzaTelemetrySplitter.UI;

/// <summary>
/// One-time welcome window shown on first run (and re-openable from the tray's "Setup guide…").
/// Walks the user through the single manual step: pointing Forza's Data Out at the splitter.
/// Reads the live <see cref="AppConfig.ListenPort"/> so the instructions stay correct even if the
/// user later changes the port.
/// </summary>
public sealed class WelcomeForm : Form
{
    private const string SetupGuideUrl =
        "https://github.com/jakemismas/Forza-Telemetry-Splitter#in-game-setup";

    private readonly AppConfig _config;
    private readonly CheckBox _dontShowAgain = new();

    public WelcomeForm(AppConfig config)
    {
        _config = config;

        Text = Strings.Welcome_Title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(460, 340);
        Font = new Font("Segoe UI", 9f);

        int x = 18, w = ClientSize.Width - 36, y = 16;

        var intro = new Label
        {
            Text = Strings.Welcome_Intro,
            AutoSize = false,
        };
        intro.SetBounds(x, y, w, 40);
        Controls.Add(intro);
        y += 48;

        var stepHeading = new Label { Text = Strings.Welcome_StepHeading, AutoSize = true };
        stepHeading.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        stepHeading.SetBounds(x, y, w, 20);
        Controls.Add(stepHeading);
        y += 24;

        var path = new Label
        {
            Text = Strings.Welcome_Path,
            AutoSize = false,
        };
        path.SetBounds(x, y, w, 20);
        Controls.Add(path);
        y += 28;

        // Copy-friendly settings block (read-only multiline text box so values can be selected/copied).
        var settings = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 245, 245),
            Font = new Font("Consolas", 10f),
            TabStop = false,
        };
        settings.Text =
            "Data Out      :  ON\r\n" +
            "IP Address    :  127.0.0.1\r\n" +
            $"Port          :  {_config.ListenPort}\r\n" +
            "Packet Format :  Car Dash (Horizon) / Dash (Motorsport)";
        settings.SetBounds(x, y, w, 92);
        Controls.Add(settings);
        y += 102;

        var note = new Label
        {
            Text = Strings.Welcome_Note,
            AutoSize = false,
            ForeColor = Color.DimGray,
        };
        note.SetBounds(x, y, w, 36);
        Controls.Add(note);
        y += 42;

        _dontShowAgain.Text = Strings.Welcome_DontShow;
        _dontShowAgain.Checked = true;
        _dontShowAgain.SetBounds(x, y, 200, 22);
        Controls.Add(_dontShowAgain);

        var guideLink = new LinkLabel { Text = Strings.Welcome_OpenGuide, AutoSize = true };
        guideLink.SetBounds(x, y + 28, 160, 20);
        guideLink.LinkClicked += (_, _) => OpenUrl(SetupGuideUrl);
        Controls.Add(guideLink);

        var getStarted = new Button { Text = Strings.Welcome_GetStarted };
        getStarted.SetBounds(ClientSize.Width - 130, ClientSize.Height - 40, 112, 28);
        getStarted.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        getStarted.DialogResult = DialogResult.OK;
        getStarted.Click += (_, _) => Close();
        Controls.Add(getStarted);

        AcceptButton = getStarted;
    }

    /// <summary>
    /// Whether the user wants to suppress the window on future launches. Honored by the caller,
    /// which persists it to <see cref="AppConfig.FirstRunComplete"/>.
    /// </summary>
    public bool DontShowAgain => _dontShowAgain.Checked;

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch
        {
            MessageBox.Show(url, "Forza Telemetry Splitter",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
