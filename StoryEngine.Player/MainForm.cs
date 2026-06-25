using StoryEngine.Engine;
using StoryEngine.Model;
using StoryEngine.Persistence;

namespace StoryEngine.Player;

public partial class MainForm : Form
{
    private StoryRuntime? _runtime;
    private Dictionary<string, byte[]> _images = new();

    // Controls
    private PictureBox      _bgPicture   = null!;
    private Panel           _hudPanel    = null!;
    private TableLayoutPanel _bottomPanel = null!;
    private Panel           _textCard    = null!;
    private RichTextBox     _storyText   = null!;
    private Panel           _choicesPanel = null!;
    private Label           _titleLabel  = null!;
    private MenuStrip       _menu        = null!;
    private Label           _chapterLabel = null!;

    public MainForm()
    {
        Text           = "Story Player";
        Size           = new Size(1100, 780);
        MinimumSize    = new Size(820, 680);
        StartPosition  = FormStartPosition.CenterScreen;
        WindowState    = FormWindowState.Normal;
        TopMost        = false;
        ShowInTaskbar  = true;
        BackColor      = Color.FromArgb(12, 10, 18);
        DoubleBuffered = true;
        BuildUi();
        ShowWelcome();

        // Forteaza aducerea ferestrei in fata, in caz ca apare in spate
        // sau pe un monitor secundar deconectat.
        this.Load += (_, _) =>
        {
            this.Activate();
            this.BringToFront();
        };
        this.Shown += (_, _) =>
        {
            this.Activate();
            this.BringToFront();
        };
    }

    // ─────────────────────────────────────────────────────────────
    // BUILD UI
    // ─────────────────────────────────────────────────────────────
    private void BuildUi()
    {
        // ── Menu ─────────────────────────────────────────────────
        _menu = new MenuStrip
        {
            BackColor = Color.FromArgb(20, 18, 28),
            ForeColor = Color.FromArgb(200, 180, 140),
            Font      = new Font("Segoe UI", 9f)
        };
        var fileMenu    = new ToolStripMenuItem("Fișier")   { ForeColor = Color.FromArgb(200,180,140) };
        var openItem    = new ToolStripMenuItem("Deschide poveste...  Ctrl+O") { ForeColor = Color.White };
        var restartItem = new ToolStripMenuItem("Repornește  F5")              { ForeColor = Color.White };
        var exitItem    = new ToolStripMenuItem("Ieșire")                      { ForeColor = Color.FromArgb(200,100,100) };

        openItem.Click    += (_, _) => OpenStory();
        restartItem.Click += (_, _) => RestartStory();
        exitItem.Click    += (_, _) => Application.Exit();

        fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            { openItem, restartItem, new ToolStripSeparator(), exitItem });
        fileMenu.DropDownOpening += (_, _) =>
        {
            foreach (ToolStripItem i in fileMenu.DropDownItems)
                i.BackColor = Color.FromArgb(28, 24, 40);
        };
        _menu.Items.Add(fileMenu);
        Controls.Add(_menu);
        MainMenuStrip = _menu;

        // ── Full-screen background image ─────────────────────────
        _bgPicture = new PictureBox
        {
            Dock      = DockStyle.Fill,
            SizeMode  = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 40)  // gri vizibil, ca sa stii daca imaginea NU s-a incarcat
        };
        Controls.Add(_bgPicture);

        // ── HUD strip ────────────────────────────────────────────
        _hudPanel = new Panel
        {
            Height    = 44,
            Dock      = DockStyle.Top,
            BackColor = Color.FromArgb(160, 10, 8, 16),
            Padding   = new Padding(12, 6, 12, 6)
        };
        Controls.Add(_hudPanel);

        // ── Title ─────────────────────────────────────────────────
        _titleLabel = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 42,
            Font      = new Font("Palatino Linotype", 15f, FontStyle.Bold | FontStyle.Italic),
            ForeColor = Color.FromArgb(230, 195, 120),
            BackColor = Color.FromArgb(180, 10, 8, 16),
            TextAlign = ContentAlignment.MiddleCenter,
            Text      = "Story Player"
        };
        Controls.Add(_titleLabel);

        // ── Bottom panel (proportional layout, never overlaps) ────
        _bottomPanel = new TableLayoutPanel
        {
            Dock        = DockStyle.Bottom,
            Height      = 380,
            ColumnCount = 1,
            RowCount    = 3,
            BackColor   = Color.Transparent,
            Padding     = new Padding(30, 10, 30, 14)
        };
        _bottomPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));   // chapter label
        _bottomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));   // text card
        _bottomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));   // choices
        _bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(_bottomPanel);

        // Chapter label (ex: "Capitolul I")
        _chapterLabel = new Label
        {
            Dock      = DockStyle.Fill,
            Font      = new Font("Segoe UI", 8f, FontStyle.Italic),
            ForeColor = Color.FromArgb(140, 120, 90),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Text      = ""
        };
        _bottomPanel.Controls.Add(_chapterLabel, 0, 0);

        // Text card
        _textCard = new Panel
        {
            Dock        = DockStyle.Fill,
            BackColor   = Color.FromArgb(195, 14, 12, 22),
            Padding     = new Padding(24, 16, 24, 16),
            Margin      = new Padding(0, 0, 0, 8)
        };
        _textCard.Paint += (s, e) =>
        {
            var r = ((Panel)s!).ClientRectangle;
            using var pen = new Pen(Color.FromArgb(60, 180, 140, 80), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
        };

        _storyText = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            BackColor   = Color.FromArgb(14, 12, 22),
            ForeColor   = Color.FromArgb(235, 228, 210),
            Font        = new Font("Palatino Linotype", 13f),
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical
        };
        _textCard.Controls.Add(_storyText);
        _bottomPanel.Controls.Add(_textCard, 0, 1);

        // Choices panel
        _choicesPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 4, 0, 0),
            AutoScroll = true
        };
        _bottomPanel.Controls.Add(_choicesPanel, 0, 2);

        // Keyboard shortcuts
        KeyPreview = true;
        KeyDown   += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.O) OpenStory();
            if (e.KeyCode == Keys.F5) RestartStory();
        };

        _hudPanel.BringToFront();
        _titleLabel.BringToFront();
        _bottomPanel.BringToFront();
        _menu.BringToFront();
    }

    // ─────────────────────────────────────────────────────────────
    // WELCOME
    // ─────────────────────────────────────────────────────────────
    private void ShowWelcome()
    {
        _storyText.Text =
            "\r\nBun venit la Story Player.\r\n\r\n" +
            "Deschide un fișier .story din meniu (Fișier → Deschide)\r\n" +
            "sau apasă  Ctrl+O.\r\n\r\n" +
            "Povestea ta te așteaptă...";
        _choicesPanel.Controls.Clear();
        _bgPicture.Image = null;
        _bgPicture.BackColor = Color.FromArgb(12, 10, 18);
    }

    // ─────────────────────────────────────────────────────────────
    // OPEN / RESTART
    // ─────────────────────────────────────────────────────────────
    private static readonly string DiagLogPath = Path.Combine(
        Path.GetTempPath(), "StoryEngine_Player_diagnostic.log");

    private static void LogDiag(string text)
    {
        try { File.AppendAllText(DiagLogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {text}\r\n"); }
        catch { /* best effort */ }
    }

    private void OpenStory()
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Deschide poveste",
            Filter = "Story files (*.story)|*.story|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            LogDiag($"OpenStory: incarcam fisierul '{dlg.FileName}'");

            // Verificare INDEPENDENTA a arhivei ZIP, separat de StoryFile.Load,
            // ca sa eliminam orice ambiguitate despre ce vede de fapt aplicatia.
            long fileSize = new FileInfo(dlg.FileName).Length;
            int zipEntryCount = 0;
            int zipImageCount = 0;
            using (var rawZip = System.IO.Compression.ZipFile.OpenRead(dlg.FileName))
            {
                zipEntryCount = rawZip.Entries.Count;
                zipImageCount = rawZip.Entries.Count(e => e.FullName.StartsWith("images/", StringComparison.OrdinalIgnoreCase));
            }
            LogDiag($"OpenStory: VERIFICARE DIRECTA ZIP -> marime fisier={fileSize} bytes, total entry-uri={zipEntryCount}, entry-uri 'images/'={zipImageCount}");

            var (story, images) = StoryFile.Load(dlg.FileName);
            LogDiag($"OpenStory: poveste incarcata cu {story.Blocks.Count} blocuri si {images.Count} imagini. Chei imagini: {string.Join(", ", images.Keys)}");

            MessageBox.Show(
                $"Fisier: {Path.GetFileName(dlg.FileName)}\n" +
                $"Marime fisier pe disc: {fileSize:N0} bytes\n" +
                $"Total entry-uri in ZIP: {zipEntryCount}\n" +
                $"Entry-uri 'images/' gasite in ZIP: {zipImageCount}\n" +
                $"Imagini incarcate de StoryFile.Load: {images.Count}\n\n" +
                (zipImageCount != images.Count
                    ? "ATENTIE: numerele NU coincid! Trimite acest mesaj."
                    : "Numerele coincid - verificare OK."),
                "Diagnostic incarcare", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _images  = images;
            _runtime = new StoryRuntime(story);
            _runtime.BlockChanged += OnBlockChanged;
            _runtime.GameOver     += OnGameOver;
            _titleLabel.Text       = story.Title;
            _runtime.Start();
        }
        catch (Exception ex)
        {
            LogDiag($"OpenStory: EROARE: {ex}");
            MessageBox.Show($"Eroare la deschidere:\n{ex.Message}\n\n{ex}", "Eroare",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestartStory()
    {
        if (_runtime == null) return;
        var story = _runtime.Definition;
        _images ??= new();
        _runtime = new StoryRuntime(story);
        _runtime.BlockChanged += OnBlockChanged;
        _runtime.GameOver     += OnGameOver;
        _runtime.Start();
    }

    // ─────────────────────────────────────────────────────────────
    // RUNTIME EVENTS
    // ─────────────────────────────────────────────────────────────
    private void OnBlockChanged(StoryBlock block, IReadOnlyList<DecisionDefinition> decisions)
    {
        if (InvokeRequired) { Invoke(() => OnBlockChanged(block, decisions)); return; }

        // Background
        LogDiag($"OnBlockChanged: block={block.Id}, bgImage={block.BackgroundImage ?? "(none)"}");
        if (!string.IsNullOrEmpty(block.BackgroundImage))
        {
            LogDiag($"  Cautam '{block.BackgroundImage}' in dictionar cu {_images.Count} imagini. Chei disponibile: {string.Join(", ", _images.Keys.Take(5))}...");
        }

        if (!string.IsNullOrEmpty(block.BackgroundImage)
            && _images.TryGetValue(block.BackgroundImage, out var imgBytes))
        {
            LogDiag($"  Gasit in dictionar: {imgBytes.Length} bytes");
            try
            {
                using var ms = new MemoryStream(imgBytes);
                using var loaded = Image.FromStream(ms);
                var bitmap = new Bitmap(loaded);
                _bgPicture.Image?.Dispose();
                _bgPicture.Image = bitmap;
                LogDiag($"  Imagine incarcata cu succes: {bitmap.Width}x{bitmap.Height}");
            }
            catch (Exception ex)
            {
                LogDiag($"  EROARE la incarcarea imaginii: {ex}");
                _bgPicture.Image = null;
                _bgPicture.BackColor = Color.FromArgb(12, 10, 18);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(block.BackgroundImage))
                LogDiag($"  NU am gasit '{block.BackgroundImage}' in dictionarul de imagini!");
            _bgPicture.Image?.Dispose();
            _bgPicture.Image    = null;
            _bgPicture.BackColor = Color.FromArgb(12, 10, 18);
        }
        // (fundalul se actualizeaza direct, fara overlay separat)

        // Chapter label from block id
        _chapterLabel.Text = FormatChapterLabel(block.Id);

        // Text
        _storyText.Text = block.Text;
        _storyText.SelectionStart = 0;
        _storyText.ScrollToCaret();

        // HUD
        UpdateHud();

        // Choices
        RebuildChoices(decisions);
    }

    private void OnGameOver(string blockId)
    {
        if (InvokeRequired) { Invoke(() => OnGameOver(blockId)); return; }
        UpdateHud();

        // Clear choices and show end screen
        _choicesPanel.Controls.Clear();

        var endLabel = new Label
        {
            Text      = "SFÂRŞIT",
            AutoSize  = false,
            Dock      = DockStyle.Top,
            Height    = 34,
            Font      = new Font("Palatino Linotype", 13f, FontStyle.Bold | FontStyle.Italic),
            ForeColor = Color.FromArgb(220, 180, 80),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _choicesPanel.Controls.Add(endLabel);

        var restartBtn = MakeChoiceButton(
            new DecisionDefinition { Text = "Joacă din nou", Icon = "" },
            Color.FromArgb(35, 55, 35),
            Color.FromArgb(80, 140, 80));
        restartBtn.Click += (_, _) => RestartStory();
        restartBtn.Dock   = DockStyle.Top;
        _choicesPanel.Controls.Add(restartBtn);
    }

    // ─────────────────────────────────────────────────────────────
    // HUD
    // ─────────────────────────────────────────────────────────────
    private void UpdateHud()
    {
        if (_runtime == null) return;
        _hudPanel.Controls.Clear();

        var props = _runtime.Definition.Properties
            .Where(p => p.VisibleInHud)
            .OrderBy(p => p.HudOrder)
            .ToList();

        int x = 10;
        foreach (var prop in props)
        {
            double val   = _runtime.State.Get(prop.Key);
            double range = prop.Max - prop.Min;
            double pct   = range > 0 ? (val - prop.Min) / range : 0;
            pct = Math.Max(0, Math.Min(1, pct)); // clamp 0..1, evita NaN/negative

            var container = new Panel
            {
                Location  = new Point(x, 4),
                Size      = new Size(140, 36),
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text      = $"{prop.HudLabel}  {(int)val}/{(int)prop.Max}",
                Location  = new Point(0, 0),
                Size      = new Size(140, 16),
                ForeColor = Color.FromArgb(200, 180, 130),
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            // Track
            var track = new Panel
            {
                Location  = new Point(0, 20),
                Size      = new Size(140, 8),
                BackColor = Color.FromArgb(40, 40, 55)
            };
            var fill = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size((int)(140 * pct), 8),
                BackColor = BarColor(prop.Key, pct)
            };
            track.Controls.Add(fill);

            container.Controls.Add(lbl);
            container.Controls.Add(track);
            _hudPanel.Controls.Add(container);

            x += 156;
        }
    }

    private static Color BarColor(string key, double pct)
    {
        // Culori specifice per proprietate
        if (key.Contains("sanatate") || key.Contains("health"))
            return pct < 0.3 ? Color.FromArgb(200, 50, 50)
                 : pct < 0.6 ? Color.FromArgb(200, 160, 40)
                 : Color.FromArgb(60, 180, 80);
        if (key.Contains("suspiciune") || key.Contains("suspicion"))
            return pct > 0.7 ? Color.FromArgb(220, 80, 40)
                 : pct > 0.4 ? Color.FromArgb(200, 150, 50)
                 : Color.FromArgb(80, 120, 200);
        if (key.Contains("indicii") || key.Contains("clues"))
            return Color.FromArgb(100, 180, 220);
        if (key.Contains("aliante") || key.Contains("allies"))
            return Color.FromArgb(140, 100, 200);
        return pct < 0.3 ? Color.FromArgb(200, 60, 60)
             : pct < 0.6 ? Color.FromArgb(200, 160, 40)
             : Color.FromArgb(60, 180, 80);
    }

    // ─────────────────────────────────────────────────────────────
    // CHOICES
    // ─────────────────────────────────────────────────────────────
    private void RebuildChoices(IReadOnlyList<DecisionDefinition> decisions)
    {
        _choicesPanel.Controls.Clear();

        // Layout: până la 4 decizii pe 2 coloane, altfel stacked
        bool twoCol = decisions.Count >= 3 && decisions.Count <= 6;

        if (twoCol)
        {
            var table = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = (int)Math.Ceiling(decisions.Count / 2.0),
                BackColor   = Color.Transparent,
                Padding     = new Padding(0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int r = 0; r < table.RowCount; r++)
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / table.RowCount));

            for (int i = 0; i < decisions.Count; i++)
            {
                var btn = MakeChoiceButton(decisions[i],
                    Color.FromArgb(28, 36, 54),
                    Color.FromArgb(70, 100, 160));
                btn.Dock   = DockStyle.Fill;
                btn.Margin = new Padding(4);
                var dec = decisions[i];
                btn.Click += (_, _) => _runtime?.Choose(dec);
                table.Controls.Add(btn, i % 2, i / 2);
            }
            _choicesPanel.Controls.Add(table);
        }
        else
        {
            foreach (var dec in decisions)
            {
                var btn = MakeChoiceButton(dec,
                    Color.FromArgb(28, 36, 54),
                    Color.FromArgb(70, 100, 160));
                btn.Height = 36;
                btn.Dock   = DockStyle.Top;
                btn.Margin = new Padding(0, 0, 0, 5);
                var d = dec;
                btn.Click += (_, _) => _runtime?.Choose(d);
                _choicesPanel.Controls.Add(btn);
            }
            // Reverse so they stack top-to-bottom
            var btns = _choicesPanel.Controls.Cast<Control>().Reverse().ToList();
            _choicesPanel.Controls.Clear();
            foreach (var b in btns) _choicesPanel.Controls.Add(b);
        }
    }

    private static Button MakeChoiceButton(DecisionDefinition dec,
        Color bgColor, Color borderColor)
    {
        string label = string.IsNullOrEmpty(dec.Icon)
            ? $"  ›  {dec.Text}"
            : $"  {dec.Icon}   {dec.Text}";

        var btn = new Button
        {
            Text      = label,
            FlatStyle = FlatStyle.Flat,
            BackColor = bgColor,
            ForeColor = Color.FromArgb(215, 210, 235),
            Font      = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderColor        = borderColor;
        btn.FlatAppearance.BorderSize         = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 60, 95);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(55, 75, 115);

        // Hover glow effect
        btn.MouseEnter += (_, _) =>
        {
            btn.ForeColor = Color.FromArgb(255, 235, 160);
            btn.FlatAppearance.BorderColor = Color.FromArgb(180, 150, 80);
        };
        btn.MouseLeave += (_, _) =>
        {
            btn.ForeColor = Color.FromArgb(215, 210, 235);
            btn.FlatAppearance.BorderColor = borderColor;
        };

        return btn;
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────
    private static string FormatChapterLabel(string blockId)
    {
        if (blockId.StartsWith("final")) return "Final";
        if (blockId.StartsWith("intro")) return "Prolog";
        return "";
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
    }
}
