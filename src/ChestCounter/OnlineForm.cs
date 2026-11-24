using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// Online connection: link this PC with the website (one click) or a manual code, open the website signed in, set a
/// public username, turn the upload on or off, and optionally sync the full personal history. Dark cards like the
/// Settings window; only the cards that apply right now are shown (not linked, or linked).
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class OnlineForm : Form
{
    private const int Pad = 20, W = 520;

    private readonly Label _state;
    private readonly Card _linkCard;
    private readonly Card _linkedCard;
    private readonly Card _userCard;
    private readonly Card _historyCard;
    private readonly Card _statusCard;
    private readonly Card _intro;
    private readonly TextBox _code;
    private readonly TextBox _user;
    private readonly ToggleSwitch _history;
    private readonly ToggleSwitch _sync;
    private readonly Label _status;
    private readonly Label _historyStatus;
    private readonly PillButton _auto;
    private readonly PillButton _open;
    private readonly PillButton _link;
    private readonly PillButton _unlink;
    private readonly PillButton _saveUser;
    private readonly PillButton _syncHistory;
    private readonly PillButton _stopHistory;
    private readonly PillButton _close;
    private bool _loading;

    public static void ShowFor(IWin32Window? owner)
    {
        using var form = new OnlineForm();
        form.ShowDialog(owner);
    }

    private OnlineForm()
    {
        Text = L.T("Conexión online", "Online connection");
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        Font = Theme.Text(9.5f);
        Icon = AppIcon.Load();

        // ---- Header ----
        Controls.Add(new Label
        {
            Text = L.T("Conexión online", "Online connection").ToUpper(L.Culture),
            Font = Fonts.Display(20f),
            ForeColor = Theme.Ink,
            BackColor = Theme.Bg,
            Location = new Point(Pad - 2, 14),
            Size = new Size(W - 150, 40),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
            AutoEllipsis = true,
        });
        _state = new Label
        {
            Font = Fonts.Body(8f, FontStyle.Bold),
            BackColor = Theme.Bg,
            Location = new Point(Pad + W - 150, 22),
            Size = new Size(150, 24),
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false,
        };
        Controls.Add(_state);

        // ---- What is uploaded ----
        _intro = NewCard(L.T("Privacidad", "Privacy"), 118);
        Note(_intro, L.T(
                "Es opcional y está desactivado por defecto. Pulsa el botón para abrir la web y aprobar este PC con un clic. Solo se suben el id de la partida, la hora, el modo y los recuentos de cofres. Nunca nombres, ids de Epic, compañeros ni replays.",
                "It is optional and off by default. Press the button to open the website and approve this PC with one click. Only the match id, start time, mode and chest counts are uploaded. Never names, Epic ids, teammates or replay files."),
            Theme.Text(9.5f), Theme.Muted, 42, 66);

        // ---- Not linked: link with one click, or with a code ----
        _linkCard = NewCard(L.T("Vincular este PC", "Link this PC"), 226);
        _history = ToggleRow(_linkCard, L.T("Subir también mi historial de partidas", "Also upload my existing match history"), 42);
        _auto = Pill(_linkCard, L.T("Abrir la web y vincular este PC", "Open website and link this PC"), new Rectangle(16, 82, W - 32, 40), primary: true);
        Note(_linkCard, L.T("Código de la web (opcional)", "Code from the website (optional)"), Theme.Text(8.5f), Theme.Muted, 136, 18);
        _code = Field(_linkCard, new Rectangle(16, 160, W - 32 - 170, 38), editable: true);
        _code.CharacterCasing = CharacterCasing.Upper;
        _code.MaxLength = 12;
        _link = Pill(_linkCard, L.T("Vincular con código", "Link with code"), new Rectangle(W - 16 - 160, 160, 160, 38));

        // ---- Linked: upload switch, open the website, unlink ----
        _linkedCard = NewCard(L.T("Este PC está vinculado", "This PC is linked"), 134);
        _sync = ToggleRow(_linkedCard, L.T("Subir cada partida al terminar", "Upload each finished match"), 42);
        _open = Pill(_linkedCard, L.T("Abrir mis estadísticas en la web", "Open my stats on the website"), new Rectangle(16, 80, W - 32 - 190, 40), primary: true);
        _unlink = Pill(_linkedCard, L.T("Desvincular este PC", "Unlink this PC"), new Rectangle(W - 16 - 180, 80, 180, 40));
        _unlink.ForeColor = Theme.Red;

        // ---- Public username ----
        _userCard = NewCard(L.T("Nombre público (clasificación)", "Public username (leaderboard)"), 96);
        _user = Field(_userCard, new Rectangle(16, 42, W - 32 - 170, 38), editable: true);
        _user.MaxLength = 20;
        _saveUser = Pill(_userCard, L.T("Guardar nombre", "Save username"), new Rectangle(W - 16 - 160, 42, 160, 38));

        // ---- Personal history (optional, separate from the leaderboard) ----
        _historyCard = NewCard(L.T("Historial personal", "Personal history"), 168);
        Note(_historyCard, L.T(
                "Opcional. Las entradas manuales y los replays importados no cuentan en la clasificación. Al sincronizar, este PC pasa a ser la fuente de tu historial personal.",
                "Optional. Manual entries and imported replays are kept out of leaderboard rankings. Syncing makes this PC the source of your personal history."),
            Theme.Text(9f), Theme.Muted, 42, 40);
        _syncHistory = Pill(_historyCard, L.T("Sincronizar mi historial completo", "Sync my full personal history"), new Rectangle(16, 88, W - 32 - 150, 38), primary: true);
        _stopHistory = Pill(_historyCard, L.T("Detener", "Stop syncing"), new Rectangle(W - 16 - 140, 88, 140, 38));
        _historyStatus = Note(_historyCard, "", Theme.Text(8.5f), Theme.Muted, 134, 22);

        // ---- Status and Close ----
        _statusCard = NewCard(null, 76);
        _status = Note(_statusCard, "", Theme.Text(9f), Theme.Muted, 10, 56);
        _status.Width = W - 32 - 130;
        _close = Pill(_statusCard, L.T("Cerrar", "Close"), new Rectangle(W - 16 - 110, 19, 110, 38), primary: true);
        _close.DialogResult = DialogResult.OK;
        AcceptButton = null;
        CancelButton = _close;

        _auto.Click += async (_, _) =>
        {
            _auto.Enabled = false;
            Say(L.T("Abriendo la web. Aprueba este PC allí y vuelve aquí.", "Opening the website. Approve this PC there, then come back here."), false);
            var error = await OnlineSync.StartLinkAsync(_history.Checked);
            if (IsDisposed)
            {
                return;
            }
            if (error != null)
            {
                _auto.Enabled = true;
                Say(error, true);
                return;
            }
            Refresh2();
        };
        _open.Click += async (_, _) =>
        {
            _open.Enabled = false;
            var error = await OnlineSync.OpenWebAsync("/stats.html");
            if (IsDisposed)
            {
                return;
            }
            _open.Enabled = true;
            if (error != null)
            {
                Say(error, true);
            }
        };
        _link.Click += async (_, _) =>
        {
            _link.Enabled = false;
            Say(L.T("Vinculando…", "Linking..."), false);
            var error = await OnlineSync.LinkAsync(_code.Text, _history.Checked);
            if (IsDisposed)
            {
                return;
            }
            _link.Enabled = true;
            if (error != null)
            {
                Say(error, true);
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
            if (IsDisposed)
            {
                return;
            }
            _saveUser.Enabled = true;
            Say(error ?? L.T("Nombre guardado.", "Username saved."), error != null);
        };
        _syncHistory.Click += async (_, _) =>
        {
            var s = OnlineSync.Load();
            if (string.IsNullOrEmpty(s.Token) || !s.Enabled)
            {
                _historyStatus.Text = L.T("Vincula este PC y activa la subida primero.", "Link this PC and turn on uploading first.");
                return;
            }
            if (!InfoDialog.Confirm(this, L.T("Sincronizar el historial personal", "Sync personal history"),
                    L.T("¿Subir todo tu historial de replays válido, las estadísticas de replays importados y los cofres añadidos a mano? No se suben notas, nombres, ids de Epic ni replays. Sustituye tu total inicial en la web en vez de sumarse. Mientras esté activado, los cambios y borrados se sincronizan.",
                        "Upload all your eligible replay history, imported replay statistics and manual chest amounts? Notes, names, Epic IDs and replay files are not uploaded. This replaces your starting total on the website rather than adding to it. Future edits and deletions sync while this is on."),
                    Array.Empty<InfoPoint>(),
                    L.T("Sincronizar", "Sync"), L.T("Cancelar", "Cancel")))
            {
                return;
            }
            _syncHistory.Enabled = false;
            _historyStatus.ForeColor = Theme.Muted;
            _historyStatus.Text = L.T("Sincronizando el historial…", "Syncing history…");
            var error = await PersonalCloud.ApproveAndSyncAsync();
            if (IsDisposed)
            {
                return;
            }
            _syncHistory.Enabled = true;
            _historyStatus.ForeColor = error != null ? Theme.Amber : Theme.Mint;
            _historyStatus.Text = error ?? L.T("Historial sincronizado. Los cambios se sincronizan cada minuto.", "History synced. Changes now sync every minute.");
            RefreshHistory();
        };
        _stopHistory.Click += (_, _) =>
        {
            PersonalCloud.Stop();
            _historyStatus.ForeColor = Theme.Muted;
            _historyStatus.Text = L.T("Detenido. La última copia sigue en la web hasta que la borres allí.", "Stopped. The last copy stays on the website until you clear it there.");
            RefreshHistory();
        };

        Refresh2();
        ActiveControl = _close;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle);
    }

    private void Say(string text, bool problem)
    {
        _status.ForeColor = problem ? Theme.Amber : Theme.Muted;
        _status.Text = text;
    }

    private void RefreshHistory()
    {
        var approved = PersonalCloud.Approved;
        _stopHistory.Enabled = approved;
        _syncHistory.Text = approved
            ? L.T("Sincronizar de nuevo", "Sync again")
            : L.T("Sincronizar mi historial completo", "Sync my full personal history");
        if (_historyStatus.Text.Length == 0)
        {
            _historyStatus.ForeColor = approved ? Theme.Mint : Theme.Muted;
            _historyStatus.Text = approved
                ? L.T("Activado: los cambios se sincronizan cada minuto.", "On: changes sync every minute.")
                : L.T("Desactivado.", "Off.");
        }
    }

    private void Refresh2()
    {
        _loading = true;
        var s = OnlineSync.Load();
        var linked = !string.IsNullOrEmpty(s.Token);
        _sync.Checked = s.Enabled;
        _auto.Enabled = !linked;
        _link.Enabled = !linked;
        _linkCard.Visible = !linked;
        _linkedCard.Visible = linked;
        _userCard.Visible = linked;
        _historyCard.Visible = linked;
        _state.Text = Fonts.Track((linked ? L.T("● Vinculado", "● Linked") : L.T("● Sin vincular", "● Not linked")).Replace("●", "").Trim().ToUpper(L.Culture));
        _state.ForeColor = linked ? Theme.Mint : Theme.Muted;
        Say(s.LastError
            ?? (!linked ? L.T("Este PC no está vinculado. No se sube nada.", "This PC is not linked. Nothing is uploaded.")
                : s.Enabled
                    ? (s.LastSyncUtc is { } t ? L.T("Activado. Última subida: ", "On. Last upload: ") + t.ToLocalTime().ToString("g", L.Culture) : L.T("Activado. Esperando la próxima partida.", "On. Waiting for the next match."))
                    : L.T("Vinculado, pero la subida está desactivada.", "Linked, but uploading is off.")),
            s.LastError != null);
        _historyStatus.Text = "";
        RefreshHistory();
        _loading = false;
        Relayout(linked);
    }

    /// <summary>Stacks the visible cards and sizes the window to them.</summary>
    private void Relayout(bool linked)
    {
        // Card.Visible reads false until the window is shown, so the layout works from the link state instead.
        var y = 66;
        var cards = linked
            ? new[] { _intro, _linkedCard, _userCard, _historyCard, _statusCard }
            : new[] { _intro, _linkCard, _statusCard };
        foreach (var card in cards)
        {
            card.Location = new Point(Pad, y);
            y = card.Bottom + 12;
        }
        ClientSize = new Size(W + Pad * 2, y - 12 + Pad);
    }

    // ---- Pieces ------------------------------------------------------------------------------------------------

    private Card NewCard(string? title, int height)
    {
        var card = new Card { Size = new Size(W, height), Radius = 16, BackColor = Theme.Card };
        if (title != null)
        {
            card.Controls.Add(new Label
            {
                Text = Fonts.Track(title.ToUpper(L.Culture)),
                Font = Fonts.Body(8f, FontStyle.Bold),
                ForeColor = Theme.AccentSoft,
                BackColor = Theme.Card,
                Location = new Point(16, 14),
                Size = new Size(W - 32, 18),
                UseMnemonic = false,
                AutoEllipsis = true,
            });
        }
        Controls.Add(card);
        return card;
    }

    /// <summary>How far a card's later rows move down because a note above them wrapped to more lines (kept in Tag).</summary>
    private static int Shift(Card card) => card.Tag is int px ? px : 0;

    private static Label Note(Card card, string text, Font font, Color colour, int top, int height)
    {
        top += Shift(card);
        var label = new Label
        {
            Text = text,
            Font = font,
            ForeColor = colour,
            BackColor = Theme.Card,
            Location = new Point(16, top),
            Size = new Size(W - 32, height),
            UseMnemonic = false,
            AutoEllipsis = true,
        };
        if (text.Length > 0)
        {
            // Long translations wrap to more lines: grow the note (and its card) so nothing is cut off.
            var need = TextRenderer.MeasureText(text, font, new Size(W - 32, int.MaxValue), TextFormatFlags.WordBreak).Height + 4;
            if (need > height)
            {
                label.Height = need;
                card.Height += need - height;
                card.Tag = Shift(card) + need - height;
            }
        }
        card.Controls.Add(label);
        return label;
    }

    private static ToggleSwitch ToggleRow(Card card, string text, int top)
    {
        top += Shift(card);
        var toggle = new ToggleSwitch { Size = new Size(44, 24), Location = new Point(W - 16 - 44, top + 2) };
        var label = new Label
        {
            Text = text,
            Font = Fonts.Semi(10f),
            ForeColor = Theme.Ink,
            BackColor = Theme.Card,
            Location = new Point(16, top),
            Size = new Size(W - 32 - 56, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
            AutoEllipsis = true,
            Cursor = Cursors.Hand,
        };
        label.Click += (_, _) => toggle.Checked = !toggle.Checked;
        card.Controls.Add(label);
        card.Controls.Add(toggle);
        return toggle;
    }

    private static PillButton Pill(Card card, string text, Rectangle bounds, bool primary = false)
    {
        bounds.Offset(0, Shift(card));
        var button = new PillButton
        {
            Text = text,
            Bounds = bounds,
            Radius = 12,
            Font = Fonts.Semi(9.5f),
        };
        if (primary)
        {
            button.BackColor = Theme.Accent;
            button.HoverColor = Theme.AccentHover;
            button.ForeColor = Theme.AccentInk;
        }
        card.Controls.Add(button);
        return button;
    }

    /// <summary>Text box without the white Windows border, inside a rounded dark field.</summary>
    private static TextBox Field(Card card, Rectangle bounds, bool editable)
    {
        bounds.Offset(0, Shift(card));
        var holder = new Card { Bounds = bounds, Radius = 11, BackColor = Theme.Panel };
        var box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Panel,
            ForeColor = Theme.Ink,
            Font = Theme.Text(10f),
            ReadOnly = !editable,
            Width = bounds.Width - 28,
        };
        box.Location = new Point(14, (bounds.Height - box.Height) / 2);
        holder.Controls.Add(box);
        holder.Click += (_, _) => box.Focus();
        holder.Cursor = Cursors.IBeam;
        card.Controls.Add(holder);
        return box;
    }
}
