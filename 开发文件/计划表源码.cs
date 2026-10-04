using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

static class Ui
{
    public static float S = 1f;

    public static int Px(double v) { return (int)Math.Round(v * S); }

    public static float Detect(Control c)
    {
        try { using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) { if (g.DpiX > 0) { return g.DpiX / 96f; } } }
        catch (Exception) { }
        try { using (Graphics g = c.CreateGraphics()) { if (g.DpiX > 0) { return g.DpiX / 96f; } } }
        catch (Exception) { }
        return 1f;
    }
}

class Plan
{
    public bool Done;
    public DateTime Created = DateTime.Now;
    public string Title = "";
    public string Note = "";
}

class ItemPanel : Panel
{
    public Plan P;
    public MainForm Host;
    public CheckBox Chk;
    public Label Lbl;
    public Label When;
    public Button Del;
    public TextBox NoteBox;
    bool expanded;

    public ItemPanel(MainForm host, Plan p)
    {
        P = p;
        Host = host;
        BackColor = Color.White;
        Margin = new Padding(0, 0, 0, Ui.Px(8));
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        Paint += PaintBorder;

        Chk = new CheckBox();
        Chk.Checked = p.Done;
        Chk.Cursor = Cursors.Hand;
        Chk.CheckedChanged += delegate
        {
            P.Done = Chk.Checked;
            StyleLabel();
            Host.AfterChange(false);
        };
        Controls.Add(Chk);

        Lbl = new Label();
        Lbl.AutoSize = false;
        Lbl.Text = p.Title;
        Lbl.Cursor = Cursors.Hand;
        Lbl.Click += delegate { Toggle(); };
        Controls.Add(Lbl);

        When = new Label();
        When.AutoSize = false;
        When.TextAlign = ContentAlignment.MiddleRight;
        When.ForeColor = Color.FromArgb(150, 157, 165);
        When.Cursor = Cursors.Hand;
        When.Text = p.Created.ToString("M月d日", CultureInfo.InvariantCulture);
        When.Click += delegate { Toggle(); };
        Controls.Add(When);

        Del = new Button();
        Del.Text = "×";
        Del.FlatStyle = FlatStyle.Flat;
        Del.FlatAppearance.BorderSize = 0;
        Del.ForeColor = Color.FromArgb(150, 157, 165);
        Del.Cursor = Cursors.Hand;
        Del.TabStop = false;
        Del.Click += delegate { Host.DeleteItem(this); };
        Controls.Add(Del);

        NoteBox = new TextBox();
        NoteBox.Multiline = true;
        NoteBox.BorderStyle = BorderStyle.FixedSingle;
        NoteBox.ScrollBars = ScrollBars.Vertical;
        NoteBox.BackColor = Color.FromArgb(250, 251, 252);
        NoteBox.Text = p.Note;
        NoteBox.Visible = false;
        NoteBox.TextChanged += delegate
        {
            P.Note = NoteBox.Text;
            Host.AfterChange(false);
        };
        Controls.Add(NoteBox);

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("重命名", null, delegate { Host.RenameItem(this); });
        menu.Items.Add("删除", null, delegate { Host.DeleteItem(this); });
        ContextMenuStrip = menu;
        Lbl.ContextMenuStrip = menu;

        StyleLabel();
        LayoutContent();
    }

    void PaintBorder(object sender, PaintEventArgs e)
    {
        using (Pen pen = new Pen(Color.FromArgb(228, 231, 235), Math.Max(1, Ui.Px(1))))
        {
            int t = Math.Max(1, Ui.Px(1));
            e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - t, ClientSize.Height - t);
        }
    }

    public void StyleLabel()
    {
        Lbl.Font = P.Done ? Host.FontStrike : Host.FontTitle;
        Lbl.ForeColor = P.Done ? Color.FromArgb(154, 163, 173) : Color.FromArgb(31, 35, 40);
    }

    public void Toggle()
    {
        expanded = !expanded;
        LayoutContent();
        Host.Relayout();
    }

    public void LayoutContent()
    {
        int w = ClientSize.Width;
        Chk.SetBounds(Ui.Px(10), Ui.Px(12), Ui.Px(18), Ui.Px(18));

        int delW = Ui.Px(26);
        int whenW = Ui.Px(54);
        int gap = Ui.Px(8);
        Del.SetBounds(w - delW - Ui.Px(6), Ui.Px(8), delW, Ui.Px(24));
        When.SetBounds(w - delW - Ui.Px(6) - gap - whenW, Ui.Px(12), whenW, Ui.Px(18));

        int textX = Ui.Px(36);
        int textW = (w - delW - Ui.Px(6) - gap - whenW - gap) - textX;
        if (textW < Ui.Px(40)) { textW = Ui.Px(40); }

        Size sz = TextRenderer.MeasureText(Lbl.Text, Lbl.Font,
            new Size(textW, Ui.Px(2000)), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        int titleH = Math.Max(Ui.Px(22), sz.Height + Ui.Px(2));
        Lbl.SetBounds(textX, Ui.Px(10), textW, titleH);

        if (expanded)
        {
            NoteBox.Visible = true;
            NoteBox.SetBounds(textX, Ui.Px(10) + titleH + Ui.Px(6), textW, Ui.Px(78));
            Height = Ui.Px(10) + titleH + Ui.Px(6) + Ui.Px(78) + Ui.Px(10);
        }
        else
        {
            NoteBox.Visible = false;
            Height = Ui.Px(10) + titleH + Ui.Px(10);
        }
    }
}

class MainForm : Form
{
    public readonly Font FontTitle = new Font("Microsoft YaHei UI", 10.5f);
    public readonly Font FontStrike = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Strikeout);

    readonly string dataPath;
    readonly List<Plan> plans = new List<Plan>();

    TextBox input;
    Button addBtn;
    FlowLayoutPanel flow;
    Label emptyLabel;
    Label stat;
    Label hint;
    Button clearBtn;
    bool relayouting;

    public MainForm()
    {
        dataPath = Path.Combine(Application.StartupPath, "计划数据.txt");
        Ui.S = Ui.Detect(this);

        Text = "我的计划表";
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch (Exception) { }
        Font = new Font("Microsoft YaHei UI", 9.5f);
        BackColor = Color.FromArgb(245, 246, 248);
        ClientSize = new Size(Ui.Px(560), Ui.Px(640));
        MinimumSize = new Size(Ui.Px(420), Ui.Px(360));
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None;

        Panel top = new Panel();
        top.Dock = DockStyle.Top;
        top.Height = Ui.Px(56);
        top.BackColor = BackColor;
        Controls.Add(top);

        input = new TextBox();
        input.Font = new Font("Microsoft YaHei UI", 10.5f);
        input.BorderStyle = BorderStyle.FixedSingle;
        input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        input.SetBounds(Ui.Px(12), Ui.Px(13), ClientSize.Width - Ui.Px(12) - Ui.Px(84) - Ui.Px(12), Ui.Px(30));
        input.KeyDown += delegate (object s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { AddItem(); e.SuppressKeyPress = true; }
        };
        top.Controls.Add(input);

        addBtn = new Button();
        addBtn.Text = "添加";
        addBtn.FlatStyle = FlatStyle.Flat;
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.BackColor = Color.FromArgb(47, 111, 235);
        addBtn.ForeColor = Color.White;
        addBtn.Cursor = Cursors.Hand;
        addBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        addBtn.SetBounds(ClientSize.Width - Ui.Px(12) - Ui.Px(84), Ui.Px(12), Ui.Px(84), Ui.Px(32));
        addBtn.Click += delegate { AddItem(); };
        top.Controls.Add(addBtn);

        Panel bottom = new Panel();
        bottom.Dock = DockStyle.Bottom;
        bottom.Height = Ui.Px(46);
        bottom.BackColor = BackColor;
        Controls.Add(bottom);

        stat = new Label();
        stat.AutoSize = true;
        stat.ForeColor = Color.FromArgb(120, 127, 135);
        stat.Location = new Point(Ui.Px(14), Ui.Px(15));
        bottom.Controls.Add(stat);

        hint = new Label();
        hint.AutoSize = true;
        hint.ForeColor = Color.FromArgb(165, 171, 179);
        hint.Location = new Point(Ui.Px(14), Ui.Px(15));
        hint.Visible = false;
        bottom.Controls.Add(hint);

        clearBtn = new Button();
        clearBtn.Text = "清除已完成";
        clearBtn.FlatStyle = FlatStyle.Flat;
        clearBtn.FlatAppearance.BorderSize = 0;
        clearBtn.ForeColor = Color.FromArgb(120, 127, 135);
        clearBtn.Cursor = Cursors.Hand;
        clearBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        clearBtn.SetBounds(ClientSize.Width - Ui.Px(12) - Ui.Px(96), Ui.Px(9), Ui.Px(96), Ui.Px(28));
        clearBtn.Click += delegate { ClearDone(); };
        bottom.Controls.Add(clearBtn);

        flow = new FlowLayoutPanel();
        flow.Dock = DockStyle.Fill;
        flow.FlowDirection = FlowDirection.TopDown;
        flow.WrapContents = false;
        flow.AutoScroll = true;
        flow.BackColor = BackColor;
        flow.Padding = new Padding(Ui.Px(12), Ui.Px(6), Ui.Px(12), Ui.Px(12));

        emptyLabel = new Label();
        emptyLabel.AutoSize = false;
        emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        emptyLabel.ForeColor = Color.FromArgb(150, 157, 165);
        emptyLabel.Text = "还没有计划，先在上面添加一条吧。";
        emptyLabel.Margin = new Padding(0, Ui.Px(20), 0, 0);
        flow.Controls.Add(emptyLabel);

        flow.Resize += delegate { Relayout(); };
        Controls.Add(flow);
        flow.BringToFront();

        LoadPlans();
        if (!File.Exists(dataPath)) { Save(); }

        foreach (Plan p in plans)
        {
            flow.Controls.Add(new ItemPanel(this, p));
        }
        Relayout();
        input.Focus();

        Application.ThreadException += delegate (object s, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("程序遇到一个小问题，已跳过，不会影响你的数据。\n\n" + e.Exception.Message,
                "我的计划表", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
    }

    public void AfterChange(bool rebuild)
    {
        Save();
        UpdateStat();
        if (rebuild) { Relayout(); }
    }

    public void AddItem()
    {
        string text = input.Text.Trim();
        if (text.Length == 0) { return; }
        Plan p = new Plan();
        p.Title = text;
        p.Created = DateTime.Now;
        plans.Add(p);
        ItemPanel item = new ItemPanel(this, p);
        flow.Controls.Add(item);
        input.Clear();
        Relayout();
        flow.ScrollControlIntoView(item);
        input.Focus();
    }

    public void DeleteItem(ItemPanel item)
    {
        if (MessageBox.Show(this, "删除「" + item.P.Title + "」？", "我的计划表",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) { return; }
        plans.Remove(item.P);
        flow.Controls.Remove(item);
        item.Dispose();
        AfterChange(true);
    }

    public void RenameItem(ItemPanel item)
    {
        string text = Microsoft.VisualBasic.Interaction.InputBox(
            "修改这条计划的内容：", "重命名", item.P.Title);
        text = text == null ? "" : text.Trim();
        if (text.Length == 0) { return; }
        item.P.Title = text;
        item.Lbl.Text = text;
        item.LayoutContent();
        AfterChange(true);
    }

    void ClearDone()
    {
        List<ItemPanel> targets = new List<ItemPanel>();
        foreach (Control c in flow.Controls)
        {
            ItemPanel it = c as ItemPanel;
            if (it != null && it.P.Done) { targets.Add(it); }
        }
        if (targets.Count == 0) { return; }
        if (MessageBox.Show(this, "清除所有已完成的计划？（共 " + targets.Count + " 条）", "我的计划表",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) { return; }
        foreach (ItemPanel it in targets)
        {
            plans.Remove(it.P);
            flow.Controls.Remove(it);
            it.Dispose();
        }
        AfterChange(true);
    }

    public void Relayout()
    {
        if (relayouting) { return; }
        relayouting = true;
        try
        {
            int w = flow.ClientSize.Width - flow.Padding.Horizontal;
            if (w < Ui.Px(120)) { w = Ui.Px(120); }
            emptyLabel.Width = w;
            emptyLabel.Height = Ui.Px(60);
            foreach (Control c in flow.Controls)
            {
                ItemPanel it = c as ItemPanel;
                if (it != null)
                {
                    it.Width = w;
                    it.LayoutContent();
                }
            }
            UpdateStat();
        }
        finally { relayouting = false; }
    }

    void UpdateStat()
    {
        int done = 0;
        foreach (Plan p in plans) { if (p.Done) { done++; } }
        stat.Text = "共 " + plans.Count + " 条 · 已完成 " + done + " 条 · 剩余 " + (plans.Count - done) + " 条";
        stat.Visible = plans.Count > 0;
        hint.Visible = plans.Count == 0;
        hint.Text = "数据自动保存在本文件夹的「计划数据.txt」里";
        emptyLabel.Visible = plans.Count == 0;
    }

    void LoadPlans()
    {
        if (!File.Exists(dataPath)) { return; }
        try
        {
            string[] lines = File.ReadAllLines(dataPath, Encoding.UTF8);
            Plan cur = null;
            bool inNote = false;
            List<string> note = new List<string>();
            foreach (string raw in lines)
            {
                string line = raw;
                if (line == "---")
                {
                    if (cur != null) { cur.Note = string.Join("\n", note.ToArray()); if (cur.Title.Length > 0) { plans.Add(cur); } }
                    cur = new Plan();
                    note.Clear();
                    inNote = false;
                    continue;
                }
                if (cur == null) { continue; }
                if (!inNote && line.StartsWith("DONE:")) { cur.Done = line.Substring(5).Trim() == "1"; continue; }
                if (!inNote && line.StartsWith("TIME:"))
                {
                    DateTime t;
                    if (DateTime.TryParse(line.Substring(5).Trim(), CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out t)) { cur.Created = t; }
                    continue;
                }
                if (!inNote && line.StartsWith("TITLE:")) { cur.Title = line.Substring(6); continue; }
                if (!inNote && line.StartsWith("NOTE:"))
                {
                    inNote = true;
                    string rest = line.Substring(5);
                    if (rest.Length > 0) { note.Add(Unescape(rest)); }
                    continue;
                }
                if (inNote) { note.Add(Unescape(line)); }
            }
            if (cur != null) { cur.Note = string.Join("\n", note.ToArray()); if (cur.Title.Length > 0) { plans.Add(cur); } }
        }
        catch (Exception)
        {
            try { File.Copy(dataPath, dataPath + ".读取失败备份.txt", true); } catch (Exception) { }
            MessageBox.Show("计划数据文件读不出来，已备份为「计划数据.txt.读取失败备份.txt」，程序按空表打开。",
                "我的计划表", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            plans.Clear();
        }
    }

    void Save()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Plan p in plans)
        {
            sb.Append("---\r\n");
            sb.Append("DONE:").Append(p.Done ? "1" : "0").Append("\r\n");
            sb.Append("TIME:").Append(p.Created.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("TITLE:").Append(p.Title).Append("\r\n");
            sb.Append("NOTE:");
            if (p.Note.Length > 0)
            {
                string[] parts = p.Note.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0) { sb.Append("\r\n"); }
                    sb.Append(Escape(parts[i]));
                }
            }
            sb.Append("\r\n");
        }
        try
        {
            File.WriteAllText(dataPath, sb.ToString(), new UTF8Encoding(true));
        }
        catch (Exception e)
        {
            MessageBox.Show("保存失败：" + e.Message, "我的计划表", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    static string Escape(string s)
    {
        if (s.StartsWith("\\") || s.StartsWith("---")) { return "\\" + s; }
        return s;
    }

    static string Unescape(string s)
    {
        if (s.StartsWith("\\")) { return s.Substring(1); }
        return s;
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
