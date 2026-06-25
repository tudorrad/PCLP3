using StoryEngine.Model;
using StoryEngine.Persistence;

namespace StoryEngine.Editor;

public partial class EditorMainForm : Form
{
    private StoryDefinition _story = new();
    private Dictionary<string, byte[]> _images = new();
    private string? _currentFile;
    private bool _dirty;

    // ── Controls ──────────────────────────────────────────
    private MenuStrip       _menu         = null!;
    private SplitContainer  _split        = null!;
    private ListBox         _blockList    = null!;
    private Panel           _editorPanel  = null!;

    // Block editor fields
    private TextBox   _blockId        = null!;
    private RichTextBox _blockText    = null!;
    private CheckBox  _isFinal        = null!;
    private Label     _bgImageLabel   = null!;
    private Button    _setBgBtn       = null!;
    private Button    _clearBgBtn     = null!;
    private DataGridView _decisionsGrid = null!;

    // Story-level fields
    private TextBox   _storyTitle     = null!;
    private TextBox   _startBlock     = null!;
    private DataGridView _propsGrid   = null!;

    // Tabs
    private TabControl _tabs          = null!;

    public EditorMainForm()
    {
        Text          = "Story Editor";
        Size          = new Size(1100, 720);
        MinimumSize   = new Size(800, 550);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor     = Color.FromArgb(22, 22, 30);
        ForeColor     = Color.White;

        BuildUi();
        NewStory();
    }

    // ──────────────────────────────────────────────────────
    // UI BUILD
    // ──────────────────────────────────────────────────────

    private void BuildUi()
    {
        // ── Menu ─────────────────────────────────────────
        _menu = new MenuStrip { BackColor = Color.FromArgb(30, 30, 42), ForeColor = Color.White };

        var fileMenu   = new ToolStripMenuItem("Fișier")  { ForeColor = Color.White };
        var newItem    = new ToolStripMenuItem("Poveste nouă (Ctrl+N)")    { ForeColor = Color.White };
        var openItem   = new ToolStripMenuItem("Deschide... (Ctrl+O)")     { ForeColor = Color.White };
        var saveItem   = new ToolStripMenuItem("Salvează (Ctrl+S)")        { ForeColor = Color.White };
        var saveAsItem = new ToolStripMenuItem("Salvează ca...")            { ForeColor = Color.White };
        var exitItem   = new ToolStripMenuItem("Ieșire")                   { ForeColor = Color.White };

        newItem.Click    += (_, _) => NewStory();
        openItem.Click   += (_, _) => OpenStory();
        saveItem.Click   += (_, _) => SaveStory();
        saveAsItem.Click += (_, _) => SaveStoryAs();
        exitItem.Click   += (_, _) => { if (ConfirmDiscard()) Application.Exit(); };

        fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            { newItem, openItem, new ToolStripSeparator(), saveItem, saveAsItem,
              new ToolStripSeparator(), exitItem });
        _menu.Items.Add(fileMenu);

        var blockMenu  = new ToolStripMenuItem("Blocuri") { ForeColor = Color.White };
        var addBlock   = new ToolStripMenuItem("Adaugă bloc")  { ForeColor = Color.White };
        var delBlock   = new ToolStripMenuItem("Șterge bloc selectat") { ForeColor = Color.White };

        addBlock.Click += (_, _) => AddBlock();
        delBlock.Click += (_, _) => DeleteBlock();

        blockMenu.DropDownItems.AddRange(new ToolStripItem[] { addBlock, delBlock });
        _menu.Items.Add(blockMenu);

        Controls.Add(_menu);
        MainMenuStrip = _menu;

        // ── Tabs ─────────────────────────────────────────
        _tabs = new TabControl
        {
            Dock      = DockStyle.Fill,
            Font      = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(22, 22, 30)
        };

        var tabBlocks = new TabPage("Blocuri") { BackColor = Color.FromArgb(22, 22, 30) };
        var tabStory  = new TabPage("Poveste / Proprietăți") { BackColor = Color.FromArgb(22, 22, 30) };

        _tabs.TabPages.Add(tabBlocks);
        _tabs.TabPages.Add(tabStory);
        Controls.Add(_tabs);

        // ── Blocks Tab ───────────────────────────────────
        _split = new SplitContainer
        {
            Dock        = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 200,
            BackColor   = Color.FromArgb(18, 18, 26)
        };
        tabBlocks.Controls.Add(_split);

        // Left: block list
        var listPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(25, 25, 35) };
        var listLabel = new Label
        {
            Text      = "Blocuri narative",
            Dock      = DockStyle.Top,
            Height    = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(35, 35, 50),
            ForeColor = Color.FromArgb(180, 160, 100),
            Font      = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        _blockList = new ListBox
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(22, 22, 32),
            ForeColor = Color.FromArgb(200, 200, 220),
            Font      = new Font("Consolas", 9),
            BorderStyle = BorderStyle.None
        };
        _blockList.SelectedIndexChanged += (_, _) => LoadSelectedBlock();

        var addBtnSide = MakeButton("+ Bloc", Color.FromArgb(30, 70, 30));
        addBtnSide.Dock   = DockStyle.Bottom;
        addBtnSide.Height = 28;
        addBtnSide.Click += (_, _) => AddBlock();

        listPanel.Controls.Add(_blockList);
        listPanel.Controls.Add(listLabel);
        listPanel.Controls.Add(addBtnSide);
        _split.Panel1.Controls.Add(listPanel);

        // Right: block editor
        _editorPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(22, 22, 30),
            Padding   = new Padding(10)
        };
        _split.Panel2.Controls.Add(_editorPanel);
        BuildBlockEditor();

        // ── Story Tab ────────────────────────────────────
        BuildStoryTab(tabStory);

        // Keyboard
        KeyPreview = true;
        KeyDown   += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S) SaveStory();
            if (e.Control && e.KeyCode == Keys.O) OpenStory();
            if (e.Control && e.KeyCode == Keys.N) NewStory();
        };
    }

    private void BuildBlockEditor()
    {
        int y = 10;

        Label MkLabel(string txt)
        {
            var l = new Label
            {
                Text      = txt,
                Location  = new Point(10, y),
                Size      = new Size(120, 20),
                ForeColor = Color.FromArgb(160, 160, 180),
                Font      = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            _editorPanel.Controls.Add(l);
            return l;
        }

        TextBox MkTextBox(int height = 22)
        {
            var t = new TextBox
            {
                Location  = new Point(140, y),
                Size      = new Size(_editorPanel.Width - 160, height),
                BackColor = Color.FromArgb(35, 35, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font      = new Font("Consolas", 9)
            };
            t.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _editorPanel.Controls.Add(t);
            return t;
        }

        // Block ID
        MkLabel("ID bloc:");
        _blockId = MkTextBox();
        _blockId.TextChanged += (_, _) => MarkDirty();
        y += 30;

        // isFinal
        _isFinal = new CheckBox
        {
            Text     = "Bloc final (sfârşit de poveste)",
            Location = new Point(140, y),
            AutoSize = true,
            ForeColor = Color.FromArgb(220, 180, 100),
            Font     = new Font("Segoe UI", 9)
        };
        _isFinal.CheckedChanged += (_, _) => MarkDirty();
        _editorPanel.Controls.Add(_isFinal);
        y += 28;

        // Background image
        MkLabel("Imagine fundal:");
        _bgImageLabel = new Label
        {
            Location  = new Point(140, y),
            Size      = new Size(260, 20),
            ForeColor = Color.FromArgb(140, 200, 140),
            Text      = "(fără imagine)"
        };
        _editorPanel.Controls.Add(_bgImageLabel);

        _setBgBtn = MakeButton("Setează...", Color.FromArgb(30, 50, 80));
        _setBgBtn.Size     = new Size(90, 22);
        _setBgBtn.Location = new Point(410, y);
        _setBgBtn.Click   += (_, _) => SetBackgroundImage();
        _editorPanel.Controls.Add(_setBgBtn);

        _clearBgBtn = MakeButton("Șterge", Color.FromArgb(70, 30, 30));
        _clearBgBtn.Size     = new Size(70, 22);
        _clearBgBtn.Location = new Point(510, y);
        _clearBgBtn.Click   += (_, _) => ClearBackgroundImage();
        _editorPanel.Controls.Add(_clearBgBtn);
        y += 30;

        // Text
        MkLabel("Text narativ:");
        _blockText = new RichTextBox
        {
            Location    = new Point(140, y),
            Size        = new Size(_editorPanel.Width - 160, 120),
            BackColor   = Color.FromArgb(35, 35, 50),
            ForeColor   = Color.FromArgb(220, 215, 200),
            BorderStyle = BorderStyle.FixedSingle,
            Font        = new Font("Georgia", 10)
        };
        _blockText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _blockText.TextChanged += (_, _) => MarkDirty();
        _editorPanel.Controls.Add(_blockText);
        y += 130;

        // Decisions grid
        var decLabel = new Label
        {
            Text      = "Decizii / opțiuni:",
            Location  = new Point(10, y),
            Size      = new Size(580, 20),
            ForeColor = Color.FromArgb(180, 160, 100),
            Font      = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _editorPanel.Controls.Add(decLabel);
        y += 24;

        _decisionsGrid = new DataGridView
        {
            Location           = new Point(10, y),
            Size               = new Size(_editorPanel.Width - 30, _editorPanel.Height - y - 50),
            Anchor             = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            BackgroundColor    = Color.FromArgb(28, 28, 40),
            GridColor          = Color.FromArgb(50, 50, 70),
            ForeColor          = Color.White,
            DefaultCellStyle   = { BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White },
            ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(40, 40, 60), ForeColor = Color.FromArgb(200,180,120), Font = new Font("Segoe UI", 8, FontStyle.Bold) },
            BorderStyle        = BorderStyle.None,
            RowHeadersVisible  = false,
            AllowUserToAddRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _decisionsGrid.CellValueChanged += (_, _) => MarkDirty();

        _decisionsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColText",   HeaderText = "Text decizie",   FillWeight = 40 });
        _decisionsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTarget", HeaderText = "Bloc destinație", FillWeight = 25 });
        _decisionsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColIcon",   HeaderText = "Icon (emoji)",   FillWeight = 10 });
        _decisionsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColEffects",HeaderText = "Efecte (prop:op:val,...)", FillWeight = 25 });

        _editorPanel.Controls.Add(_decisionsGrid);

        var saveBlockBtn = MakeButton("Salvează blocul", Color.FromArgb(30, 70, 30));
        saveBlockBtn.Size     = new Size(160, 30);
        saveBlockBtn.Anchor   = AnchorStyles.Bottom | AnchorStyles.Right;
        saveBlockBtn.Location = new Point(_editorPanel.Width - 180, _editorPanel.Height - 38);
        saveBlockBtn.Click   += (_, _) => SaveCurrentBlock();
        _editorPanel.Controls.Add(saveBlockBtn);
    }

    private void BuildStoryTab(TabPage tab)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(22, 22, 30) };
        tab.Controls.Add(panel);

        int y = 20;

        void AddRow(string lbl, Control ctrl)
        {
            var l = new Label
            {
                Text      = lbl,
                Location  = new Point(10, y + 3),
                Size      = new Size(160, 20),
                ForeColor = Color.FromArgb(180, 160, 100),
                Font      = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            ctrl.Location = new Point(180, y);
            ctrl.Size     = new Size(400, 24);
            panel.Controls.Add(l);
            panel.Controls.Add(ctrl);
            y += 36;
        }

        _storyTitle  = new TextBox { BackColor = Color.FromArgb(35, 35, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
        _startBlock  = new TextBox { BackColor = Color.FromArgb(35, 35, 50), ForeColor = Color.White, Font = new Font("Consolas", 9),  BorderStyle = BorderStyle.FixedSingle };

        _storyTitle.TextChanged += (_, _) => MarkDirty();
        _startBlock.TextChanged += (_, _) => MarkDirty();

        AddRow("Titlu poveste:", _storyTitle);
        AddRow("Bloc de start:", _startBlock);

        y += 10;
        var propsLabel = new Label
        {
            Text      = "Proprietăți stare (statistici personaj):",
            Location  = new Point(10, y),
            AutoSize  = true,
            ForeColor = Color.FromArgb(200, 180, 120),
            Font      = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        panel.Controls.Add(propsLabel);
        y += 26;

        _propsGrid = new DataGridView
        {
            Location            = new Point(10, y),
            Size                = new Size(900, 300),
            Anchor              = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor     = Color.FromArgb(28, 28, 40),
            GridColor           = Color.FromArgb(50, 50, 70),
            ForeColor           = Color.White,
            DefaultCellStyle    = { BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White },
            ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(40, 40, 60), ForeColor = Color.FromArgb(200,180,120), Font = new Font("Segoe UI", 8, FontStyle.Bold) },
            BorderStyle         = BorderStyle.None,
            RowHeadersVisible   = false,
            AllowUserToAddRows  = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _propsGrid.CellValueChanged += (_, _) => MarkDirty();

        _propsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PKey",      HeaderText = "Cheie",       FillWeight = 14 });
        _propsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PHud",      HeaderText = "Label HUD",   FillWeight = 14 });
        _propsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PMin",      HeaderText = "Min",         FillWeight = 8 });
        _propsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PMax",      HeaderText = "Max",         FillWeight = 8 });
        _propsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PInit",     HeaderText = "Inițial",     FillWeight = 8 });
        _propsGrid.Columns.Add(new DataGridViewCheckBoxColumn{ Name = "PHudVis",   HeaderText = "Vis. HUD",    FillWeight = 8 });
        _propsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "POnMin",    HeaderText = "Bloc la Min", FillWeight = 20 });
        _propsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "POnMax",    HeaderText = "Bloc la Max", FillWeight = 20 });

        panel.Controls.Add(_propsGrid);
        y += 310;

        var saveStoryBtn = MakeButton("Salvează meta-date", Color.FromArgb(30, 70, 30));
        saveStoryBtn.Location = new Point(10, y);
        saveStoryBtn.Size     = new Size(200, 32);
        saveStoryBtn.Click   += (_, _) => SaveStoryMeta();
        panel.Controls.Add(saveStoryBtn);
    }

    // ──────────────────────────────────────────────────────
    // STORY OPERATIONS
    // ──────────────────────────────────────────────────────

    private void NewStory()
    {
        if (!ConfirmDiscard()) return;
        _story       = new StoryDefinition { Title = "Poveste nouă", StartBlock = "start" };
        _images      = new();
        _currentFile = null;
        _dirty       = false;
        RefreshAll();
    }

    private void OpenStory()
    {
        if (!ConfirmDiscard()) return;
        using var dlg = new OpenFileDialog
        {
            Title  = "Deschide poveste",
            Filter = "Story files (*.story)|*.story"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            var (story, images) = StoryFile.Load(dlg.FileName);
            _story       = story;
            _images      = images;
            _currentFile = dlg.FileName;
            _dirty       = false;
            RefreshAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eroare:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveStory()
    {
        if (_currentFile == null) { SaveStoryAs(); return; }
        CommitCurrentBlock();
        CommitStoryMeta();
        try
        {
            StoryFile.SaveWithBytes(_currentFile, _story, _images);
            _dirty    = false;
            UpdateTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eroare salvare:\n{ex.Message}", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveStoryAs()
    {
        using var dlg = new SaveFileDialog
        {
            Title      = "Salvează poveste",
            Filter     = "Story files (*.story)|*.story",
            DefaultExt = "story",
            FileName   = _story.Title
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _currentFile = dlg.FileName;
        SaveStory();
    }

    // ──────────────────────────────────────────────────────
    // BLOCK OPERATIONS
    // ──────────────────────────────────────────────────────

    private void AddBlock()
    {
        string id = $"bloc_{_story.Blocks.Count + 1}";
        _story.Blocks.Add(new StoryBlock { Id = id, Text = "Textul blocului..." });
        RefreshBlockList();
        _blockList.SelectedItem = id;
        MarkDirty();
    }

    private void DeleteBlock()
    {
        if (_blockList.SelectedItem is not string id) return;
        if (MessageBox.Show($"Ștergi blocul '{id}'?", "Confirmare",
            MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        _story.Blocks.RemoveAll(b => b.Id == id);
        RefreshBlockList();
        MarkDirty();
    }

    private StoryBlock? _currentEditBlock;

    private void LoadSelectedBlock()
    {
        if (_blockList.SelectedItem is not string id) return;
        var block = _story.Blocks.FirstOrDefault(b => b.Id == id);
        if (block == null) return;

        _currentEditBlock = block;

        _blockId.Text   = block.Id;
        _blockText.Text = block.Text;
        _isFinal.Checked = block.IsFinal;
        _bgImageLabel.Text = string.IsNullOrEmpty(block.BackgroundImage)
            ? "(fără imagine)" : block.BackgroundImage;

        _decisionsGrid.Rows.Clear();
        foreach (var dec in block.Decisions)
        {
            string effects = string.Join(",",
                dec.Effects.Select(e => $"{e.Property}:{e.Type}:{e.Value}"));
            _decisionsGrid.Rows.Add(dec.Text, dec.TargetBlock, dec.Icon ?? "", effects);
        }
    }

    private void SaveCurrentBlock()
    {
        CommitCurrentBlock();
        MessageBox.Show("Bloc salvat în memorie.\nFolosește Ctrl+S pentru a salva fișierul.",
            "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void CommitCurrentBlock()
    {
        if (_currentEditBlock == null) return;

        string newId = _blockId.Text.Trim();
        if (!string.IsNullOrEmpty(newId) && newId != _currentEditBlock.Id)
        {
            // Actualizeaza referintele
            foreach (var blk in _story.Blocks)
                foreach (var dec in blk.Decisions)
                    if (dec.TargetBlock == _currentEditBlock.Id)
                        dec.TargetBlock = newId;

            if (_story.StartBlock == _currentEditBlock.Id)
                _story.StartBlock = newId;

            _currentEditBlock.Id = newId;
        }

        _currentEditBlock.Text    = _blockText.Text;
        _currentEditBlock.IsFinal = _isFinal.Checked;

        // Decizii din grid
        _currentEditBlock.Decisions.Clear();
        foreach (DataGridViewRow row in _decisionsGrid.Rows)
        {
            string? text   = row.Cells["ColText"].Value?.ToString();
            string? target = row.Cells["ColTarget"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(target)) continue;

            var dec = new DecisionDefinition
            {
                Text        = text,
                TargetBlock = target,
                Icon        = row.Cells["ColIcon"].Value?.ToString()
            };

            string? effectsStr = row.Cells["ColEffects"].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(effectsStr))
            {
                foreach (var part in effectsStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var tokens = part.Split(':');
                    if (tokens.Length == 3 && double.TryParse(tokens[2], out double val))
                        dec.Effects.Add(new EffectDefinition
                            { Property = tokens[0].Trim(), Type = tokens[1].Trim().ToUpper(), Value = val });
                }
            }

            _currentEditBlock.Decisions.Add(dec);
        }

        RefreshBlockList();
    }

    private void SaveStoryMeta() => CommitStoryMeta();

    private void CommitStoryMeta()
    {
        _story.Title      = _storyTitle.Text.Trim();
        _story.StartBlock = _startBlock.Text.Trim();

        _story.Properties.Clear();
        foreach (DataGridViewRow row in _propsGrid.Rows)
        {
            string? key = row.Cells["PKey"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(key)) continue;

            var prop = new StatePropertyDefinition
            {
                Key          = key,
                HudLabel     = row.Cells["PHud"].Value?.ToString() ?? key,
                Min          = ParseDouble(row.Cells["PMin"].Value, 0),
                Max          = ParseDouble(row.Cells["PMax"].Value, 100),
                Initial      = ParseDouble(row.Cells["PInit"].Value, 0),
                VisibleInHud = row.Cells["PHudVis"].Value is true,
                OnMinBlock   = row.Cells["POnMin"].Value?.ToString(),
                OnMaxBlock   = row.Cells["POnMax"].Value?.ToString()
            };
            _story.Properties.Add(prop);
        }
        MarkDirty();
        MessageBox.Show("Meta-date salvate în memorie.\nFolosește Ctrl+S pentru a salva fișierul.",
            "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ──────────────────────────────────────────────────────
    // BACKGROUND IMAGE
    // ──────────────────────────────────────────────────────

    private void SetBackgroundImage()
    {
        if (_currentEditBlock == null) { MessageBox.Show("Selectați mai întâi un bloc."); return; }
        using var dlg = new OpenFileDialog
        {
            Title  = "Selectează imagine fundal",
            Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        string name  = Path.GetFileName(dlg.FileName);
        byte[] bytes = File.ReadAllBytes(dlg.FileName);
        _images[name] = bytes;
        _currentEditBlock.BackgroundImage = name;
        _bgImageLabel.Text = name;
        MarkDirty();
    }

    private void ClearBackgroundImage()
    {
        if (_currentEditBlock == null) return;
        _currentEditBlock.BackgroundImage = null;
        _bgImageLabel.Text = "(fără imagine)";
        MarkDirty();
    }

    // ──────────────────────────────────────────────────────
    // REFRESH
    // ──────────────────────────────────────────────────────

    private void RefreshAll()
    {
        _storyTitle.Text = _story.Title;
        _startBlock.Text = _story.StartBlock;

        _propsGrid.Rows.Clear();
        foreach (var p in _story.Properties)
            _propsGrid.Rows.Add(p.Key, p.HudLabel, p.Min, p.Max, p.Initial,
                p.VisibleInHud, p.OnMinBlock, p.OnMaxBlock);

        RefreshBlockList();
        UpdateTitle();

        _currentEditBlock = null;
        _blockId.Text   = "";
        _blockText.Text = "";
        _isFinal.Checked = false;
        _bgImageLabel.Text = "(fără imagine)";
        _decisionsGrid.Rows.Clear();
    }

    private void RefreshBlockList()
    {
        string? sel = _blockList.SelectedItem?.ToString();
        _blockList.Items.Clear();
        foreach (var b in _story.Blocks)
            _blockList.Items.Add(b.Id);
        if (sel != null && _blockList.Items.Contains(sel))
            _blockList.SelectedItem = sel;
    }

    // ──────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────

    private void MarkDirty() { _dirty = true; UpdateTitle(); }

    private void UpdateTitle()
    {
        Text = $"Story Editor – {_story.Title}{(_dirty ? " *" : "")}{(_currentFile != null ? $" [{_currentFile}]" : "")}";
    }

    private bool ConfirmDiscard()
    {
        if (!_dirty) return true;
        var r = MessageBox.Show("Ai modificări nesalvate. Continui?", "Confirmare",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        return r == DialogResult.Yes;
    }

    private static double ParseDouble(object? val, double def)
        => double.TryParse(val?.ToString(), out var d) ? d : def;

    private static Button MakeButton(string text, Color bg)
    {
        var btn = new Button
        {
            Text      = text,
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9),
            Cursor    = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
        return btn;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!ConfirmDiscard()) e.Cancel = true;
        base.OnFormClosing(e);
    }
}
