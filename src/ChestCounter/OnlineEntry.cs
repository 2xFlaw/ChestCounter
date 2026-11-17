using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// Adds an "Online connection" button to the Settings window without editing SettingsForm.cs.
/// Temporary: it can be moved into SettingsForm later. Watches for the Settings window when the UI is idle.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class OnlineEntry
{
    private const string Tag = "online-entry";
    private static bool _installed;

    public static void Install()
    {
        if (_installed)
        {
            return;
        }
        _installed = true;
        Application.Idle += (_, _) =>
        {
            foreach (var form in Application.OpenForms.OfType<SettingsForm>())
            {
                if (form.Controls.OfType<PillButton>().Any(b => b.Tag as string == Tag))
                {
                    continue;
                }
                var button = new PillButton
                {
                    Text = L.T("Conexión online", "Online connection"),
                    Location = new Point(110, 9),
                    Size = new Size(160, 26),
                    Radius = 9,
                    Font = Theme.Text(8.5f, FontStyle.Bold),
                    Tag = Tag,
                };
                button.Click += (_, _) => OnlineForm.ShowFor(form);
                form.Controls.Add(button);
                button.BringToFront();
            }
        };
    }
}
