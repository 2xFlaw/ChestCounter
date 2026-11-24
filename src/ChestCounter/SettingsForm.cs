using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// Ajustes con el mismo aspecto que la ventana principal: dos columnas de tarjetas, títulos pequeños en gris y
/// cifras grandes de color. Cada cambio se aplica y se guarda al instante.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SettingsForm : Form
{
    private static readonly string[] UpdateLabels = { L.T("Preguntar", "Ask"), L.T("Automáticas", "Automatic"), L.T("Nunca", "Never") };
    private static readonly string[] UpdateModes = { "prompt", "auto", "off" };
    private static readonly string[] UpdateHints =
    {
        L.T("Te pregunta antes de instalar una versión nueva", "Asks before installing a new version"),
        L.T("Instala las versiones nuevas entre partidas", "Installs new versions between matches"),
        L.T("No busca versiones nuevas", "Never checks for new versions"),
    };

    // Two columns of cards under a header, and a footer bar with the author, Reset statistics and Close.
    private const int Pad = 20, Gap = 14, ColW = 362;
    private const int LeftX = Pad, RightX = Pad + ColW + Gap, FullW = ColW * 2 + Gap;
    private const int MaxInitial = 1_000_000;
    private const int DefaultUpdateMode = 1; // "auto"

    private readonly Monitor _monitor;
    private readonly ToggleSwitch _fortnite;
    private readonly ToggleSwitch _report;
    private readonly PillButton[] _updates = new PillButton[UpdateModes.Length];
    private readonly TextBox _folder;
    private readonly TextBox _initial;
    private Label? _updateHint;
    private bool _loading;

    public SettingsForm(Monitor monitor, Store store)
    {
        _monitor = monitor;
        var config = monitor.Config;

        Text = L.T("Ajustes", "Settings");
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Icon = AppIcon.Load();

        // ---- Header: big title and version on the left, Online connection and language on the right ----------
        var title = new Label
        {
            Text = L.T("Ajustes", "Settings").ToUpper(L.Culture),
            Font = Fonts.Display(22f),
            ForeColor = Theme.Ink,
            BackColor = Theme.Bg,
            Location = new Point(Pad - 2, 12),
            Size = new Size(360, 42),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
        };
        Controls.Add(title);
        Lbl(this, L.F("Versión {0}", "Version {0}", AppInfo.VersionText), 9f, FontStyle.Regular, Theme.Muted,
            new Point(Pad, 54), new Size(300, 20));

        var language = AddLanguagePicker(config.Language, new Point(Pad + FullW - 190, 22), new Size(190, 34));
        var online = new PillButton
        {
            Text = L.T("Conexión online", "Online connection"),
            Size = new Size(190, 34),
            Location = new Point(language.Left - 10 - 190, 22),
            Radius = 11,
            Font = Fonts.Semi(9.5f),
            Indicator = OnlineSync.Load() is { } link && !string.IsNullOrEmpty(link.Token) ? Theme.Mint : null,
        };
        online.Click += (_, _) =>
        {
            OnlineForm.ShowFor(this);
            online.Indicator = !string.IsNullOrEmpty(OnlineSync.Load().Token) ? Theme.Mint : null;
        };
        Controls.Add(online);

        // ---- Left column ------------------------------------------------------------------------------------
        var y = 86;

        var start = AddCard(LeftX, y, ColW, 226);
        CardTitle(start, L.T("Opciones", "Options"));
        _fortnite = AddToggleRow(start, L.T("Abrir con Fortnite", "Open with Fortnite"),
            L.T("Cuenta tus partidas sin pensar en ello", "Counts your matches on its own"), 44, config.StartWithFortnite);
        _fortnite.CheckedChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }
            if (!_monitor.SetStartWithFortnite(_fortnite.Checked))
            {
                _loading = true;
                _fortnite.Checked = !_fortnite.Checked;
                _loading = false;
                MessageBox.Show(this,
                    L.T("No se puede desde aquí: abre el contador con ChestCounter.exe.", "Not possible from here: open the counter with ChestCounter.exe."),
                    L.T("Ajustes", "Settings"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
        _report = AddToggleRow(start, L.T("Abrir el informe al iniciar", "Open the report at startup"), null, 92, config.OpenReportOnStart);
        _report.CheckedChanged += (_, _) => { if (!_loading) _monitor.Change(c => c.OpenReportOnStart = _report.Checked); };
        var mates = AddToggleRow(start, L.T("Compañeros en el informe", "Teammates in the report"),
            L.T("Sus cofres: a unos 250 m de ti", "Their chests: within about 250 m of you"), 130, config.ShowTeammates);
        mates.CheckedChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }
            if (mates.Checked && !InfoDialog.Confirm(this, L.T("Compañeros en el informe", "Teammates in the report"),
                    L.T("La ventana y el informe mostrarán con quién juegas y los cofres de cada compañero.",
                        "The window and the report will show who you play with and each teammate's chests."),
                    new[] { ExactPoint(), ApproxPoint() },
                    L.T("Activar", "Turn on"), L.T("Cancelar", "Cancel")))
            {
                Revert(mates);
                return;
            }
            _monitor.SetShowTeammates(mates.Checked);
        };
        var addMates = AddToggleRow(start, L.T("Sumar sus cofres a tu total", "Add their chests to your total"), null, 178, config.CountTeammateChests);
        addMates.CheckedChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }
            if (addMates.Checked && !InfoDialog.Confirm(this, L.T("Sumar sus cofres a tu total", "Add their chests to your total"),
                    L.T("Tu total, tu media, tus récords y el overlay incluirán también los cofres de tus compañeros.",
                        "Your total, your average, your records and the overlay will also include your teammates' chests."),
                    new[]
                    {
                        ApproxPoint(),
                        new InfoPoint("↺", Theme.Cyan, L.T("Tu historial no cambia", "Your history does not change"),
                            L.T("Al desactivarlo vuelves a ver solo tus cofres.", "Turning it off shows only your chests again.")),
                    },
                    L.T("Activar", "Turn on"), L.T("Cancelar", "Cancel")))
            {
                Revert(addMates);
                return;
            }
            _monitor.SetCountTeammates(addMates.Checked);
        };
        y = start.Bottom + Gap;

        // Also count as a chest: ammo boxes, food boxes and loot cannons (broken chests always count).
        var extras = AddCard(LeftX, y, ColW, 158);
        CardTitle(extras, L.T("Contar también como cofre", "Also count as a chest"));
        var ammo = AddToggleRow(extras, L.T("Cajas de munición", "Ammo boxes"), null, 44, config.CountAmmoBoxes);
        ammo.CheckedChanged += (_, _) => { if (!_loading) _monitor.SetCountExtra(c => c.CountAmmoBoxes = ammo.Checked); };
        var food = AddToggleRow(extras, L.T("Cajas de comida", "Food boxes"), null, 82, config.CountFoodBoxes);
        food.CheckedChanged += (_, _) => { if (!_loading) _monitor.SetCountExtra(c => c.CountFoodBoxes = food.Checked); };
        var cannons = AddToggleRow(extras, L.T("Cañones de botín", "Loot cannons"), null, 120, config.CountLootCannons);
        cannons.CheckedChanged += (_, _) => { if (!_loading) _monitor.SetCountExtra(c => c.CountLootCannons = cannons.Checked); };
        y = extras.Bottom + Gap;

        // Replays folder, and where the data is saved.
        var folderCard = AddCard(LeftX, y, ColW, 136);
        CardTitle(folderCard, L.T("Carpeta de replays", "Replays folder"));
        _folder = AddField(folderCard, new Point(16, 42), ColW - 32 - 96, config.DemosFolder);
        var browse = new PillButton { Text = L.T("Cambiar", "Change"), Location = new Point(ColW - 16 - 88, 42), Size = new Size(88, 34), Radius = 11, Font = Fonts.Semi(9.5f) };
        browse.Click += (_, _) => ChooseFolder();
        folderCard.Controls.Add(browse);
        var saved = Lbl(folderCard, L.T("Tus datos se guardan en ", "Your data is saved in ") + store.Folder,
            8.5f, FontStyle.Regular, Theme.Muted, new Point(16, 86), new Size(ColW - 32, 38));
        saved.AutoEllipsis = false; // two lines: a long path wraps instead of being cut
        var leftBottom = folderCard.Bottom;

        // ---- Right column -----------------------------------------------------------------------------------
        y = 86;

        // Chests before the counter: big editable number with − / +.
        var initial = AddCard(RightX, y, ColW, 138);
        CardTitle(initial, L.T("Cofres antes del contador", "Chests before the counter"));
        var numberHolder = new Card { Location = new Point(16, 40), Size = new Size(ColW - 32 - 92, 52), Radius = 12, BackColor = Theme.Panel };
        _initial = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Panel,
            ForeColor = Theme.Amber,
            Font = Fonts.Display(22f),
            Width = numberHolder.Width - 28,
            MaxLength = 7,
            Text = Math.Min(MaxInitial, monitor.InitialChests).ToString(L.Culture),
        };
        _initial.Location = new Point(14, (numberHolder.Height - _initial.Height) / 2);
        numberHolder.Controls.Add(_initial);
        initial.Controls.Add(numberHolder);
        var minus = new PillButton { Text = "−", Location = new Point(ColW - 16 - 84, 49), Size = new Size(40, 34), Radius = 11, Font = Theme.Text(12f, FontStyle.Bold) };
        var plus = new PillButton { Text = "+", Location = new Point(ColW - 16 - 40, 49), Size = new Size(40, 34), Radius = 11, Font = Theme.Text(12f, FontStyle.Bold) };
        plus.Click += (_, _) => StepInitial(10);
        minus.Click += (_, _) => StepInitial(-10);
        initial.Controls.Add(plus);
        initial.Controls.Add(minus);
        _initial.KeyPress += (_, e) => e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
        _initial.TextChanged += (_, _) =>
        {
            if (!_loading)
            {
                _monitor.SetInitialChests(ParseInitial());
            }
        };
        var profileName = monitor.ShownProfileName;
        Lbl(initial, profileName != null
                ? L.F("estimado · se suma al total de {0}", "estimate · added to {0}'s total", profileName)
                : L.T("estimado · se suma a tu total", "estimate · added to your total"),
            8.5f, FontStyle.Regular, Theme.Muted, new Point(16, 102), new Size(ColW - 32, 22));
        y = initial.Bottom + Gap;

        // Updates: three pills and a line explaining the chosen one.
        var updates = AddCard(RightX, y, ColW, 122);
        CardTitle(updates, L.T("Actualizaciones", "Updates"));
        var pillW = (ColW - 32 - 2 * 8) / 3;
        for (var i = 0; i < UpdateModes.Length; i++)
        {
            var index = i;
            var pill = new PillButton { Text = UpdateLabels[i], Location = new Point(16 + i * (pillW + 8), 42), Size = new Size(pillW, 34), Radius = 11, Font = Fonts.Semi(9.5f) };
            pill.Click += (_, _) => SelectUpdateMode(index, save: true);
            updates.Controls.Add(pill);
            _updates[i] = pill;
        }
        _updateHint = Lbl(updates, UpdateHints[DefaultUpdateMode], 8.5f, FontStyle.Regular, Theme.Muted, new Point(16, 86), new Size(ColW - 32, 22));
        var mode = Array.IndexOf(UpdateModes, config.UpdateMode);
        SelectUpdateMode(mode < 0 ? DefaultUpdateMode : mode, save: false);
        y = updates.Bottom + Gap;

        // Overlay for OBS: address, state and Copy.
        var obsCard = AddCard(RightX, y, ColW, 96);
        CardTitle(obsCard, L.T("Overlay para OBS", "Overlay for OBS"));
        var obsUrl = ObsServer.Url;
        var stateText = obsUrl != null ? L.T("● ACTIVO", "● ON") : L.T("● NO", "● OFF");
        var stateCard = new Card { Location = new Point(ColW - 16 - 78, 12), Size = new Size(78, 22), Radius = 11, BackColor = Color.FromArgb(36, obsUrl != null ? Theme.Mint : Theme.Red) };
        stateCard.BackColor = Fx.Mix(Theme.Card, obsUrl != null ? Theme.Mint : Theme.Red, 0.16f);
        Lbl(stateCard, stateText, 8f, FontStyle.Bold, obsUrl != null ? Theme.Mint : Theme.Red, new Point(0, 0), new Size(78, 22), ContentAlignment.MiddleCenter)
            .BackColor = Color.Transparent;
        obsCard.Controls.Add(stateCard);
        var obs = AddField(obsCard, new Point(16, 44), ColW - 32 - 96, obsUrl ?? L.T("Puerto ocupado", "Port in use"));
        var copy = new PillButton { Text = L.T("Copiar", "Copy"), Location = new Point(ColW - 16 - 88, 44), Size = new Size(88, 34), Radius = 11, Font = Fonts.Semi(9.5f), Enabled = obsUrl != null };
        copy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(obsUrl!);
                copy.Text = "✓";
            }
            catch
            {
                obs.Focus();
                obs.SelectAll();
            }
        };
        obsCard.Controls.Add(copy);
        y = obsCard.Bottom + Gap;

        // Overlay look, personal chest goal and the overlay shortcuts.
        var look = AddCard(RightX, y, ColW, Math.Max(leftBottom - y, 226));
        CardTitle(look, L.T("Overlay y objetivo", "Overlay & goal"));
        var apOpacity = Math.Clamp(config.OverlayOpacity, 40, 100);
        var apGoal = Math.Max(0, config.ChestGoal);
        void ApStepRow(string text, int top, Func<string> value, Action<int> step)
        {
            LblF(look, text, Fonts.Semi(10f), Theme.Ink, new Point(16, top), new Size(ColW - 32 - 84 - 72, 34), ContentAlignment.MiddleLeft);
            var apValue = LblF(look, value(), Fonts.Semi(10f), Theme.Amber, new Point(ColW - 16 - 84 - 68, top), new Size(60, 34), ContentAlignment.MiddleRight);
            var apDown = new PillButton { Text = "−", Location = new Point(ColW - 16 - 84, top), Size = new Size(40, 34), Radius = 11, Font = Theme.Text(12f, FontStyle.Bold) };
            var apUp = new PillButton { Text = "+", Location = new Point(ColW - 16 - 40, top), Size = new Size(40, 34), Radius = 11, Font = Theme.Text(12f, FontStyle.Bold) };
            apDown.Click += (_, _) => { step(-1); apValue.Text = value(); };
            apUp.Click += (_, _) => { step(1); apValue.Text = value(); };
            look.Controls.Add(apDown);
            look.Controls.Add(apUp);
        }
        ApStepRow(L.T("Opacidad del overlay", "Overlay opacity"), 40, () => apOpacity + "%", d =>
        {
            apOpacity = Math.Clamp(apOpacity + d * 10, 40, 100);
            _monitor.Change(c => c.OverlayOpacity = apOpacity);
            NativeOverlay.SetOpacity(apOpacity);
        });
        ApStepRow(L.T("Objetivo de cofres", "Chest goal"), 80, () => apGoal == 0 ? L.T("Sin objetivo", "Off") : apGoal.ToString("N0", L.Culture), d =>
        {
            apGoal = Math.Clamp(apGoal + d * 100, 0, 100000);
            _monitor.Change(c => c.ChestGoal = apGoal);
        });
        var apCompact = AddToggleRow(look, L.T("Overlay compacto", "Compact overlay"),
            L.T("Solo total y partida actual", "Total and match counter only"), 126, config.OverlayCompact);
        apCompact.CheckedChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }
            _monitor.Change(c => c.OverlayCompact = apCompact.Checked);
            NativeOverlay.SetCompact(apCompact.Checked);
        };
        var shortcutsTop = 176;
        Lbl(look, L.T("Atajos del overlay", "Overlay shortcuts"), 8.5f, FontStyle.Regular, Theme.Muted, new Point(16, shortcutsTop), new Size(ColW - 32, 18));
        LblF(look, L.T("Ctrl+Mayús+O mostrar · L fijar · K compacto", "Ctrl+Shift+O show · L lock · K compact"),
            Fonts.Semi(9f), Theme.Ink, new Point(16, shortcutsTop + 18), new Size(ColW - 32, 20));
        var apReset = new PillButton { Text = L.T("Restablecer posición", "Reset position"), Location = new Point(ColW - 16 - 140, 10), Size = new Size(140, 28), Radius = 10, Font = Fonts.Semi(8.5f) };
        apReset.Click += (_, _) => NativeOverlay.ResetPosition();
        look.Controls.Add(apReset);
        apReset.BringToFront();

        // ---- Footer: author on the left, Reset statistics and Close on the right -----------------------------
        var footerTop = Math.Max(look.Bottom, leftBottom) + Gap + 4;
        var footer = new Card { Location = new Point(LeftX, footerTop), Size = new Size(FullW, 56), Radius = 14, BackColor = Theme.Card };
        Controls.Add(footer);
        AddLinks(footer, L.T("Hecho por", "Made by"), "2xFlaw", "https://github.com/2xFlaw/ChestCounter",
            Array.Empty<(string Tip, string Url, string Logo)>());

        var close = new PillButton
        {
            Text = L.T("Cerrar", "Close"),
            Size = new Size(140, 38),
            Location = new Point(FullW - 10 - 140, 9),
            Radius = 12,
            BackColor = Theme.Accent,
            HoverColor = Theme.AccentHover,
            ForeColor = Theme.AccentInk,
            Font = Fonts.Semi(10f),
            DialogResult = DialogResult.OK,
        };
        footer.Controls.Add(close);

        // Reset statistics: deletes the history (with a confirmation and a backup copy).
        var reset = new PillButton
        {
            Text = L.T("Restablecer estadísticas…", "Reset statistics…"),
            Size = new Size(190, 38),
            Location = new Point(close.Left - 10 - 190, 9),
            Radius = 12,
            Font = Fonts.Semi(9.5f),
            ForeColor = Theme.Red,
        };
        reset.Click += (_, _) => ResetStats();
        footer.Controls.Add(reset);

        ClientSize = new Size(Pad * 2 + FullW, footer.Bottom + Pad);

        FitTexts(this);
        AcceptButton = close;
        CancelButton = close;
        ActiveControl = close; // no focus ring on the first switch when it opens
    }

    /// <summary>
    /// Selector de idioma: «Sistema» (el de Windows, por defecto) o uno de los idiomas, con su nombre en ese idioma.
    /// El cambio se guarda al momento y se aplica al reiniciar el contador (se ofrece hacerlo ya).
    /// </summary>
    private PillButton AddLanguagePicker(string? current, Point at, Size size)
    {
        var chosen = current != null && L.Languages.Any(l => l.Code == current) ? current : null;
        string NameOf(string? code) => code == null
            ? L.F("Sistema ({0})", "System ({0})", L.Languages.FirstOrDefault(l => l.Code == L.SystemCode).Name)
            : L.Languages.First(l => l.Code == code).Name;

        // Botón píldora como los de la ventana; al pulsarlo, un menú oscuro con los idiomas.
        var button = new PillButton
        {
            Text = NameOf(chosen) ,
            Location = at,
            Size = size,
            Radius = 11,
            Font = Fonts.Semi(9.5f),
        };
        var menu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            BackColor = Theme.Panel,
            Font = Theme.Text(9f),
            Padding = new Padding(4),
        };
        void AddItem(string? code)
        {
            var item = new ToolStripMenuItem((code == chosen ? "✓   " : "      ") + NameOf(code))
            {
                ForeColor = code == chosen ? Theme.Cyan : Theme.Ink,
                Padding = new Padding(4, 3, 18, 3),
            };
            item.Click += (_, _) => Choose(code);
            menu.Items.Add(item);
        }
        AddItem(null);
        menu.Items.Add(new ToolStripSeparator());
        foreach (var (code, _) in L.Languages)
        {
            AddItem(code);
        }
        button.ContextMenuStrip = menu; // también con clic derecho
        button.Click += (_, _) => menu.Show(button, new Point(0, button.Height + 4));
        Controls.Add(button);

        void Choose(string? code)
        {
            if (code == chosen)
            {
                return;
            }
            chosen = code;
            button.Text = NameOf(code) ;
            foreach (var item in menu.Items.OfType<ToolStripMenuItem>())
            {
                var mine = item.Text?.TrimStart('✓', ' ') == NameOf(code);
                item.Text = (mine ? "✓   " : "      ") + item.Text?.TrimStart('✓', ' ');
                item.ForeColor = mine ? Theme.Cyan : Theme.Ink;
            }
            _monitor.Change(c => c.Language = code);
            if (InfoDialog.Confirm(this, L.T("Idioma", "Language"),
                    L.T("El nuevo idioma se aplica al reiniciar el contador. Tu historial y tus ajustes se conservan.",
                        "The new language applies when the counter restarts. Your history and settings are kept."),
                    Array.Empty<InfoPoint>(),
                    L.T("Reiniciar ahora", "Restart now"), L.T("Más tarde", "Later")))
            {
                Close();
                _monitor.Restart();
            }
        }
        return button;
    }

    /// <summary>
    /// Textos que no caben en su sitio (idiomas largos): la letra se reduce un poco, hasta 6,5 pt, en vez de cortarse.
    /// Solo afecta a etiquetas de una línea y a botones.
    /// </summary>
    private static void FitTexts(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            FitTexts(c);
            if (c is not (Label { AutoEllipsis: true } or PillButton) || string.IsNullOrEmpty(c.Text))
            {
                continue;
            }
            var font = c.Font;
            if (c is Label && c.Height > font.Height * 1.8)
            {
                continue; // varias líneas: ya tiene sitio para partirse
            }
            var room = c.Width - (c is PillButton ? 12 : 2);
            while (font.Size > 6.5f && TextRenderer.MeasureText(c.Text, font).Width > room)
            {
                font = new Font(font.FontFamily, font.Size - 0.25f, font.Style);
            }
            if (!ReferenceEquals(font, c.Font))
            {
                c.Font = font;
            }
        }
    }

    /// <summary>Lo que sí es exacto de los compañeros.</summary>
    private static InfoPoint ExactPoint() => new("✓", Theme.Mint, L.T("Exacto", "Exact"),
        L.T("Con quién juegas, partidas juntos y victorias.", "Who you play with, matches together and wins."));

    /// <summary>Lo que no: sus cofres, con la distancia medida en replays reales.</summary>
    private static InfoPoint ApproxPoint() => new("≈", Theme.Amber, L.T("Aproximado: sus cofres", "Approximate: their chests"),
        L.Big("teammatesRange",
            "Fortnite solo graba que se abre un cofre a menos de unos 250 m de ti. Los que un compañero abre más lejos no están en el replay, así que son un mínimo, no su total. El informe muestra cuánto de cada partida pasó cerca de ti.",
            "Fortnite only records a chest being opened within about 250 m of you. Chests a teammate opens further away are not in the replay, so their count is a minimum, not their total. The report shows how much of each match they spent near you."));

    /// <summary>Vuelve a poner el interruptor como estaba (se canceló el diálogo), sin guardar nada.</summary>
    private void Revert(ToggleSwitch toggle)
    {
        _loading = true;
        toggle.Checked = !toggle.Checked;
        _loading = false;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle);
    }

    private void SelectUpdateMode(int index, bool save)
    {
        for (var i = 0; i < _updates.Length; i++)
        {
            var on = i == index;
            _updates[i].BackColor = on ? Theme.Accent : Theme.Panel;
            _updates[i].HoverColor = on ? Theme.AccentHover : Theme.PanelHover;
            _updates[i].ForeColor = on ? Theme.AccentInk : Theme.Ink;
            _updates[i].Invalidate();
        }
        if (_updateHint != null)
        {
            _updateHint.Text = UpdateHints[index];
        }
        if (save)
        {
            var chosen = UpdateModes[index];
            _monitor.Change(c => c.UpdateMode = chosen);
        }
    }

    private int ParseInitial() =>
        int.TryParse(_initial.Text, System.Globalization.NumberStyles.None, L.Culture, out var n) ? Math.Clamp(n, 0, MaxInitial) : 0;

    private void StepInitial(int delta)
    {
        var value = Math.Clamp(ParseInitial() + delta, 0, MaxInitial);
        _initial.Text = value.ToString(L.Culture); // TextChanged guarda el valor
    }

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = L.T("Carpeta de replays de Fortnite", "Fortnite replays folder"),
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_folder.Text) ? _folder.Text : "",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _monitor.ChangeDemosFolder(dialog.SelectedPath);
            _folder.Text = dialog.SelectedPath;
        }
    }

    // ---- Piezas de la ventana ----------------------------------------------------------------

    private Card AddCard(int x, int y, int w, int h)
    {
        var card = new Card { Location = new Point(x, y), Size = new Size(w, h), Radius = 16, BackColor = Theme.Card };
        Controls.Add(card);
        return card;
    }

    /// <summary>
    /// Pie: título gris y el enlace en cian a la izquierda, y botones con los logos a la derecha (si hay);
    /// cada uno abre su URL en el navegador. Los logos son PNG incrustados en el ejecutable (carpeta Assets).
    /// </summary>
    private static void AddLinks(Card footer, string title, string webText, string webUrl, (string Tip, string Url, string Logo)[] networks)
    {
        // Textos medidos y centrados en vertical: así no se tapan entre sí ni se cortan en ningún idioma.
        var titleFont = Theme.Text(9.5f);
        var titleW = TextRenderer.MeasureText(title, titleFont).Width;
        Lbl(footer, title, 9.5f, FontStyle.Regular, Theme.Muted, new Point(16, 2), new Size(titleW, footer.Height - 4), ContentAlignment.MiddleLeft);

        var webFont = Fonts.Semi(10.5f);
        var webLabel = webText + "  ↗";
        var web = new LinkLabel
        {
            Text = webLabel,
            Font = webFont,
            LinkColor = Theme.Amber,
            ActiveLinkColor = Theme.AccentSoft,
            VisitedLinkColor = Theme.Amber,
            LinkBehavior = LinkBehavior.HoverUnderline,
            BackColor = footer.BackColor,
            Location = new Point(16 + titleW + 4, 2),
            Size = new Size(TextRenderer.MeasureText(webLabel, webFont).Width + 4, footer.Height - 4),
            TextAlign = ContentAlignment.MiddleLeft,
            LinkArea = new LinkArea(0, webLabel.Length),
            Cursor = Cursors.Hand,
        };
        web.LinkClicked += (_, _) => OpenUrl(webUrl);
        footer.Controls.Add(web);

        var tips = new ToolTip();
        tips.SetToolTip(web, webUrl);
        const int size = 30, gap = 6;
        var top = (footer.Height - size) / 2;
        var x = footer.Width - 7 - (networks.Length * size + (networks.Length - 1) * gap);
        foreach (var network in networks)
        {
            var url = network.Url;
            var button = new LogoButton
            {
                Logo = LoadLogo(network.Logo),
                Location = new Point(x, top),
                Size = new Size(size, size),
                AccessibleName = network.Tip,
            };
            button.Click += (_, _) => OpenUrl(url);
            tips.SetToolTip(button, network.Tip);
            footer.Controls.Add(button);
            x += size + gap;
        }
    }

    private static Image? LoadLogo(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"ChestCounter.Assets.{name}.png");
        if (stream == null)
        {
            return null;
        }
        var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return new Bitmap(new MemoryStream(bytes.ToArray()));
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
        }
        catch
        {
            // Sin navegador disponible: no hacemos nada.
        }
    }

    /// <summary>Card title: small wide-spaced capitals, like the section labels on the dashboard and the website.</summary>
    private static void CardTitle(Card card, string text) =>
        LblF(card, Fonts.Track(text.ToUpper(L.Culture)), Fonts.Body(8f, FontStyle.Bold), Theme.AccentSoft, new Point(16, 14), new Size(card.Width - 120, 18));

    /// <summary>Pide confirmación y borra todo el historial (se guarda una copia de data.json antes).</summary>
    private void ResetStats()
    {
        var answer = MessageBox.Show(this,
            L.T("Se borrarán todas tus partidas, cofres y tesoros guardados, y los cofres de antes del contador. El contador empezará de cero con tu próxima partida (los replays que ya tienes no se vuelven a contar).\n\nSe guarda una copia de seguridad en la carpeta de tus datos.\n\n¿Restablecer las estadísticas?",
                "All your saved matches, chests and treasures will be deleted, as well as the chests before the counter. The counter starts from zero with your next match (the replays you already have are not counted again).\n\nA backup copy is saved in your data folder.\n\nReset the statistics?"),
            L.T("Restablecer estadísticas", "Reset statistics"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes)
        {
            return;
        }
        var backup = _monitor.ResetStats();
        _loading = true;
        _initial.Text = "0";
        _loading = false;
        MessageBox.Show(this, L.F("Estadísticas restablecidas. Copia de seguridad: {0}", "Statistics reset. Backup copy: {0}", Path.GetFileName(backup)),
            L.T("Restablecer estadísticas", "Reset statistics"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>A title (and an optional grey line) on the left and a switch on the right; clicking the text toggles it too.</summary>
    private static ToggleSwitch AddToggleRow(Card card, string text, string? hint, int top, bool value)
    {
        var height = hint != null ? 40 : 28;
        var toggle = new ToggleSwitch { Size = new Size(44, 24), Location = new Point(card.Width - 16 - 44, top + (height - 24) / 2), Checked = value };
        var label = LblF(card, text, Fonts.Semi(10f), Theme.Ink, new Point(16, top + (hint != null ? 0 : 3)), new Size(card.Width - 32 - 56, 21));
        label.Cursor = Cursors.Hand;
        label.Click += (_, _) => toggle.Checked = !toggle.Checked;
        if (hint != null)
        {
            var small = Lbl(card, hint, 8.5f, FontStyle.Regular, Theme.Muted, new Point(16, top + 21), new Size(card.Width - 32 - 56, 18));
            small.Cursor = Cursors.Hand;
            small.Click += (_, _) => toggle.Checked = !toggle.Checked;
        }
        card.Controls.Add(toggle);
        return toggle;
    }

    /// <summary>Campo de solo lectura sin borde dentro de un fondo redondeado.</summary>
    private static TextBox AddField(Card card, Point at, int width, string text)
    {
        var holder = new Card { Location = at, Size = new Size(width, 34), Radius = 11, BackColor = Theme.Panel };
        var box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Panel,
            ForeColor = Theme.Ink,
            Font = Theme.Text(9.5f),
            ReadOnly = true,
            Text = text,
            Width = width - 24,
        };
        box.Location = new Point(12, (holder.Height - box.Height) / 2);
        holder.Controls.Add(box);
        card.Controls.Add(holder);
        return box;
    }

    private static Label Lbl(Control parent, string text, float size, FontStyle style, Color color, Point at, Size sz, ContentAlignment align = ContentAlignment.TopLeft) =>
        LblF(parent, text, Theme.Text(size, style), color, at, sz, align);

    private static Label LblF(Control parent, string text, Font font, Color color, Point at, Size sz, ContentAlignment align = ContentAlignment.TopLeft)
    {
        var label = new Label
        {
            Text = text,
            Font = font,
            UseMnemonic = false,
            ForeColor = color,
            BackColor = parent.BackColor,
            Location = at,
            Size = sz,
            TextAlign = align,
            AutoEllipsis = true,
        };
        parent.Controls.Add(label);
        return label;
    }
}
