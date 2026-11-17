using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>Small dialog: link this PC with the code from the website, set a username, and turn the upload on or off.</summary>
[SupportedOSPlatform("windows")]
internal sealed class OnlineForm : Form
{
    private readonly TextBox _code = new() { Width = 200, CharacterCasing = CharacterCasing.Upper, MaxLength = 12 };
    private readonly TextBox _user = new() { Width = 200, MaxLength = 20 };
    private readonly CheckBox _history = new() { AutoSize = true, Text = L.T("Subir también mi historial de partidas", "Also upload my existing match history") };
    private readonly CheckBox _sync = new() { AutoSize = true, Text = L.T("Subir cada partida al terminar", "Upload each finished match") };
    private readonly Label _status = new() { AutoSize = false, Width = 440, Height = 54 };
    private readonly Button _link = new() { AutoSize = true, Text = L.T("Vincular este PC", "Link this PC") };
    private readonly Button _unlink = new() { AutoSize = true, Text = L.T("Desvincular este PC", "Unlink this PC") };
    private readonly Button _saveUser = new() { AutoSize = true, Text = L.T("Guardar nombre", "Save username") };
    private bool _loading;

    public static void ShowFor(IWin32Window? owner)
    {
        using var form = new OnlineForm();
        form.ShowDialog(owner);
    }

    private OnlineForm()
    {
        Text = L.T("Conexión online", "Online connection");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        Font = Theme.Text(9f);
        ClientSize = new Size(480, 470);

        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(18) };
        var intro = new Label
        {
            AutoSize = false, Width = 440, Height = 92,
            Text = L.T(
                "Es opcional y está desactivado por defecto. Entra en la web, pulsa «Obtener código» y escríbelo aquí. Solo se suben el id de la partida, la hora, el modo y los recuentos de cofres. Nunca nombres, ids de Epic, compañeros ni replays.",
                "It is optional and off by default. Open the website, press \"Get link code\" and type it here. Only the match id, start time, mode and chest counts are uploaded. Never names, Epic ids, teammates or replay files."),
        };
        panel.Controls.Add(intro);
        panel.Controls.Add(new Label { AutoSize = true, Text = L.T("Código de la web", "Code from the website"), ForeColor = Theme.Muted });
        panel.Controls.Add(_code);
        panel.Controls.Add(_history);
        panel.Controls.Add(_link);
        panel.Controls.Add(_sync);
        panel.Controls.Add(_unlink);
        panel.Controls.Add(new Label { AutoSize = true, Margin = new Padding(3, 14, 3, 3), Text = L.T("Nombre público (clasificación)", "Public username (leaderboard)"), ForeColor = Theme.Muted });
        panel.Controls.Add(_user);
        panel.Controls.Add(_saveUser);
        panel.Controls.Add(_status);
        Controls.Add(panel);

        _link.Click += async (_, _) =>
        {
            _link.Enabled = false;
            _status.Text = L.T("Vinculando…", "Linking...");
            var error = await OnlineSync.LinkAsync(_code.Text, _history.Checked);
            _link.Enabled = true;
            if (error != null)
            {
                _status.Text = error;
                return;
            }
            _code.Clear();
            Refresh2();
        };
        _unlink.Click += (_, _) =>
        {
            OnlineSync.Unlink();
            Refresh2();
        };
        _sync.CheckedChanged += (_, _) =>
        {
            if (!_loading)
            {
                OnlineSync.SetEnabled(_sync.Checked);
                Refresh2();
            }
        };
        _saveUser.Click += async (_, _) =>
        {
            _saveUser.Enabled = false;
            var error = await OnlineUsername.SetAsync(_user.Text);
            _saveUser.Enabled = true;
            _status.ForeColor = error != null ? Theme.Amber : Theme.Muted;
            _status.Text = error ?? L.T("Nombre guardado.", "Username saved.");
        };
        Refresh2();
    }

    private void Refresh2()
    {
        _loading = true;
        var s = OnlineSync.Load();
        var linked = !string.IsNullOrEmpty(s.Token);
        _sync.Enabled = linked;
        _sync.Checked = s.Enabled;
        _unlink.Enabled = linked;
        _code.Enabled = !linked;
        _history.Enabled = !linked;
        _link.Enabled = !linked;
        _user.Enabled = linked;
        _saveUser.Enabled = linked;
        _status.ForeColor = s.LastError != null ? Theme.Amber : Theme.Muted;
        _status.Text = s.LastError
            ?? (!linked ? L.T("Este PC no está vinculado. No se sube nada.", "This PC is not linked. Nothing is uploaded.")
                : s.Enabled
                    ? (s.LastSyncUtc is { } t ? L.T("Activado. Última subida: ", "On. Last upload: ") + t.ToLocalTime().ToString("g") : L.T("Activado. Esperando la próxima partida.", "On. Waiting for the next match."))
                    : L.T("Vinculado, pero la subida está desactivada.", "Linked, but uploading is off."));
        _loading = false;
    }
}
