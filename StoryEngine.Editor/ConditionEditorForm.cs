using StoryEngine.Model;

namespace StoryEngine.Editor;

/// <summary>
/// Dialog pentru editarea vizuală a condițiilor pe arbore (AST).
/// Conform §14.3 din cerințe: editor pe arbore cu COMPARISON / AND / OR.
/// </summary>
public sealed class ConditionEditorForm : Form
{
    // ── Date ──────────────────────────────────────────────
    private readonly List<string> _propKeys;
    private ConditionNode? _root;
    private bool _syncing;

    // ── Controale ─────────────────────────────────────────
    private TreeView _tree = null!;
    private Panel _editPanel = null!;
    private Panel _cmpPanel = null!;   // editare COMPARISON
    private Panel _cpdPanel = null!;   // AND / OR (read-only)
    private Panel _emptyPanel = null!;   // nicio condiție
    private ComboBox _propCombo = null!;
    private ComboBox _opCombo = null!;
    private TextBox _valBox = null!;
    private Button _delBtn = null!;

    /// <summary>Condiția rezultată după apăsarea OK.</summary>
    public ConditionNode? Result { get; private set; }

    public ConditionEditorForm(ConditionNode? initial, List<string> propertyKeys)
    {
        _propKeys = propertyKeys;
        _root = DeepClone(initial);

        Text = "Editor Condiție";
        Size = new Size(720, 520);
        MinimumSize = new Size(600, 420);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(22, 22, 30);
        ForeColor = Color.White;

        BuildUi();
        RebuildTree();
    }

    // ──────────────────────────────────────────────────────
    // BUILD UI
    // ──────────────────────────────────────────────────────

    private void BuildUi()
    {
        // ── Toolbar ───────────────────────────────────────
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(30, 30, 45),
            Padding = new Padding(5, 5, 5, 0)
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var addCmpBtn = Btn("＋ COMPARISON", Color.FromArgb(25, 70, 25));
        var addAndBtn = Btn("＋ AND", Color.FromArgb(25, 50, 90));
        var addOrBtn = Btn("＋ OR", Color.FromArgb(65, 35, 90));
        _delBtn = Btn("✕ Șterge nod", Color.FromArgb(85, 25, 25));

        addCmpBtn.Click += (_, _) => AddNode("COMPARISON");
        addAndBtn.Click += (_, _) => AddNode("AND");
        addOrBtn.Click += (_, _) => AddNode("OR");
        _delBtn.Click += (_, _) => DeleteSelectedNode();

        flow.Controls.AddRange(new Control[]
            { addCmpBtn, addAndBtn, addOrBtn, _delBtn });
        toolbar.Controls.Add(flow);

        // ── Split: stânga TreeView / dreapta panou editare ─
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 280,
            BackColor = Color.FromArgb(18, 18, 26)
        };

        _tree = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(25, 25, 38),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9),
            FullRowSelect = true,
            HideSelection = false
        };
        _tree.AfterSelect += (_, _) => ShowNodeEditor();
        split.Panel1.Controls.Add(_tree);

        // ── Panoul dreapta ─────────────────────────────────
        _editPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(28, 28, 42),
            Padding = new Padding(16)
        };

        BuildComparisonPanel();
        BuildCompoundPanel();
        BuildEmptyPanel();

        _editPanel.Controls.AddRange(new Control[]
            { _cmpPanel, _cpdPanel, _emptyPanel });
        ShowOnly(_emptyPanel);

        split.Panel2.Controls.Add(_editPanel);

        // ── Butoane OK / Anulează ──────────────────────────
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            BackColor = Color.FromArgb(30, 30, 45)
        };

        var okBtn = Btn("OK", Color.FromArgb(25, 85, 25));
        var canBtn = Btn("Anulează", Color.FromArgb(85, 25, 25));
        okBtn.Size = canBtn.Size = new Size(110, 30);
        okBtn.Anchor = canBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        okBtn.Location = new Point(footer.Width - 240, 8);
        canBtn.Location = new Point(footer.Width - 122, 8);
        footer.Resize += (_, _) =>
        {
            okBtn.Left = footer.Width - 240;
            canBtn.Left = footer.Width - 122;
        };
        okBtn.Click += (_, _) => { Result = _root; DialogResult = DialogResult.OK; Close(); };
        canBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        footer.Controls.AddRange(new Control[] { okBtn, canBtn });

        Controls.Add(split);
        Controls.Add(toolbar);
        Controls.Add(footer);
    }

    private void BuildComparisonPanel()
    {
        _cmpPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        var title = Lbl("Condiție simplă (COMPARISON)", 0,
            Color.FromArgb(190, 170, 110), bold: true);
        _cmpPanel.Controls.Add(title);

        _cmpPanel.Controls.Add(Lbl("Proprietate:", 38));
        _propCombo = new ComboBox
        {
            Location = new Point(120, 36),
            Size = new Size(220, 24),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = new Font("Consolas", 9)
        };
        if (_propKeys.Count > 0)
            _propCombo.Items.AddRange(_propKeys.Cast<object>().ToArray());
        _propCombo.TextChanged += (_, _) => SyncNode();
        _propCombo.SelectedIndexChanged += (_, _) => SyncNode();
        _cmpPanel.Controls.Add(_propCombo);

        _cmpPanel.Controls.Add(Lbl("Operator:", 72));
        _opCombo = new ComboBox
        {
            Location = new Point(120, 70),
            Size = new Size(100, 24),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Consolas", 9)
        };
        _opCombo.Items.AddRange(new object[] { ">=", "<=", ">", "<", "==", "!=" });
        _opCombo.SelectedIndex = 0;
        _opCombo.SelectedIndexChanged += (_, _) => SyncNode();
        _cmpPanel.Controls.Add(_opCombo);

        _cmpPanel.Controls.Add(Lbl("Valoare:", 106));
        _valBox = new TextBox
        {
            Location = new Point(120, 104),
            Size = new Size(110, 24),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9),
            Text = "0"
        };
        _valBox.TextChanged += (_, _) => SyncNode();
        _cmpPanel.Controls.Add(_valBox);

        var hint = new Label
        {
            Text = "Exemplu: sanatate >= 20",
            Location = new Point(0, 142),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 130, 100),
            Font = new Font("Segoe UI", 8, FontStyle.Italic)
        };
        _cmpPanel.Controls.Add(hint);
    }

    private void BuildCompoundPanel()
    {
        _cpdPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _cpdPanel.Controls.Add(new Label
        {
            Text = "Nod AND / OR\n\nAdaugă sub-condiții folosind\nbutoanele din bara de sus.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(160, 160, 190),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10)
        });
    }

    private void BuildEmptyPanel()
    {
        _emptyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _emptyPanel.Controls.Add(new Label
        {
            Text = "Nicio condiție\n\nFolosește butoanele de sus\npentru a adăuga noduri.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(120, 120, 150),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10)
        });
    }

    // ──────────────────────────────────────────────────────
    // ARBORE
    // ──────────────────────────────────────────────────────

    private void RebuildTree()
    {
        _tree.Nodes.Clear();
        if (_root == null)
        {
            ShowOnly(_emptyPanel);
            return;
        }
        _tree.Nodes.Add(BuildTreeNode(_root));
        _tree.ExpandAll();
        _tree.SelectedNode = _tree.Nodes[0];
    }

    private static TreeNode BuildTreeNode(ConditionNode cn)
    {
        string label = cn.Type switch
        {
            "COMPARISON" => $"{cn.Property} {cn.Operator} " +
                            $"{cn.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            "AND" => $"AND  ({cn.Conditions?.Count ?? 0} ramuri)",
            "OR" => $"OR   ({cn.Conditions?.Count ?? 0} ramuri)",
            _ => cn.Type
        };
        Color color = cn.Type switch
        {
            "COMPARISON" => Color.FromArgb(140, 205, 140),
            "AND" => Color.FromArgb(110, 165, 220),
            "OR" => Color.FromArgb(195, 145, 215),
            _ => Color.White
        };
        var tn = new TreeNode(label) { Tag = cn, ForeColor = color };
        if (cn.Conditions != null)
            foreach (var child in cn.Conditions)
                tn.Nodes.Add(BuildTreeNode(child));
        return tn;
    }

    private void UpdateTreeNodeLabel(TreeNode tn, ConditionNode cn)
    {
        tn.Text = cn.Type switch
        {
            "COMPARISON" => $"{cn.Property} {cn.Operator} " +
                            $"{cn.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            "AND" => $"AND  ({cn.Conditions?.Count ?? 0} ramuri)",
            "OR" => $"OR   ({cn.Conditions?.Count ?? 0} ramuri)",
            _ => cn.Type
        };
    }

    // ──────────────────────────────────────────────────────
    // SELECTARE NOD → PANOU EDITARE
    // ──────────────────────────────────────────────────────

    private void ShowNodeEditor()
    {
        if (_tree.SelectedNode?.Tag is not ConditionNode cn)
        {
            ShowOnly(_emptyPanel);
            return;
        }

        if (cn.Type == "COMPARISON")
        {
            ShowOnly(_cmpPanel);
            _syncing = true;
            _propCombo.Text = cn.Property ?? "";
            _opCombo.SelectedItem = cn.Operator;
            if (_opCombo.SelectedIndex < 0) _opCombo.SelectedIndex = 0;
            _valBox.Text = cn.Value?.ToString(
                System.Globalization.CultureInfo.InvariantCulture) ?? "0";
            _syncing = false;
        }
        else
        {
            ShowOnly(_cpdPanel);
        }

        _delBtn.Enabled = true;
    }

    private void SyncNode()
    {
        if (_syncing) return;
        if (_tree.SelectedNode?.Tag is not ConditionNode cn
            || cn.Type != "COMPARISON") return;

        cn.Property = _propCombo.Text.Trim();
        cn.Operator = _opCombo.SelectedItem?.ToString() ?? ">=";
        cn.Value = double.TryParse(_valBox.Text,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

        UpdateTreeNodeLabel(_tree.SelectedNode, cn);
    }

    // ──────────────────────────────────────────────────────
    // ADĂUGARE / ȘTERGERE NOD
    // ──────────────────────────────────────────────────────

    private void AddNode(string type)
    {
        var newCn = type == "COMPARISON"
            ? new ConditionNode
            {
                Type = "COMPARISON",
                Property = _propKeys.FirstOrDefault() ?? "",
                Operator = ">=",
                Value = 0
            }
            : new ConditionNode
            {
                Type = type,
                Conditions = new List<ConditionNode>()
            };

        var selTn = _tree.SelectedNode;
        var selCn = selTn?.Tag as ConditionNode;

        // Cazul 1: nicio condiție existentă → noul nod devine rădăcina
        if (_root == null)
        {
            _root = newCn;
            RebuildTree();
            SelectByTag(newCn);
            return;
        }

        // Cazul 2: nodul selectat e AND/OR → adaugă copil
        if (selCn?.Type is "AND" or "OR")
        {
            selCn.Conditions!.Add(newCn);
            RebuildTree();
            SelectByTag(newCn);
            return;
        }

        // Cazul 3: rădăcina e AND/OR și nimic sau COMPARISON selectat
        if (_root.Type is "AND" or "OR")
        {
            _root.Conditions!.Add(newCn);
            RebuildTree();
            SelectByTag(newCn);
            return;
        }

        // Cazul 4: rădăcina e COMPARISON → înfășoară în AND([root, newCn])
        _root = new ConditionNode
        {
            Type = "AND",
            Conditions = new List<ConditionNode> { _root, newCn }
        };
        RebuildTree();
        SelectByTag(newCn);
    }

    private void DeleteSelectedNode()
    {
        var selTn = _tree.SelectedNode;
        if (selTn == null) return;

        // Șterge rădăcina
        if (selTn.Parent == null)
        {
            _root = null;
            RebuildTree();
            ShowOnly(_emptyPanel);
            return;
        }

        // Șterge copil
        if (selTn.Parent.Tag is ConditionNode parentCn
            && parentCn.Conditions != null
            && selTn.Tag is ConditionNode cn)
        {
            parentCn.Conditions.Remove(cn);
            RebuildTree();
            SelectByTag(parentCn);
        }
    }

    private void SelectByTag(ConditionNode target)
    {
        SearchAndSelect(_tree.Nodes, target);
    }

    private bool SearchAndSelect(TreeNodeCollection nodes, ConditionNode target)
    {
        foreach (TreeNode tn in nodes)
        {
            if (tn.Tag == target) { _tree.SelectedNode = tn; return true; }
            if (SearchAndSelect(tn.Nodes, target)) return true;
        }
        return false;
    }

    // ──────────────────────────────────────────────────────
    // UTILITAR
    // ──────────────────────────────────────────────────────

    private void ShowOnly(Panel p)
    {
        _cmpPanel.Visible = p == _cmpPanel;
        _cpdPanel.Visible = p == _cpdPanel;
        _emptyPanel.Visible = p == _emptyPanel;
    }

    private Label Lbl(string txt, int y,
        Color? color = null, bool bold = false)
        => new Label
        {
            Text = txt,
            Location = new Point(0, y),
            AutoSize = true,
            ForeColor = color ?? Color.FromArgb(160, 160, 180),
            Font = new Font("Segoe UI", 9,
                bold ? FontStyle.Bold : FontStyle.Regular)
        };

    private static Button Btn(string txt, Color bg)
        => new Button
        {
            Text = txt,
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(3, 0, 3, 0),
            Cursor = Cursors.Hand
        };

    private static ConditionNode? DeepClone(ConditionNode? n)
    {
        if (n == null) return null;
        return new ConditionNode
        {
            Type = n.Type,
            Property = n.Property,
            Operator = n.Operator,
            Value = n.Value,
            Conditions = n.Conditions?
                .Select(DeepClone)
                .Where(c => c != null)
                .Cast<ConditionNode>()
                .ToList()
        };
    }
}