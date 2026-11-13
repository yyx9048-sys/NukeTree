using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AppNuke3D
{
    static class Theme
    {
        public static Color Bg = Color.FromArgb(27, 29, 31);
        public static Color BgTop = Color.FromArgb(32, 35, 38);
        public static Color BgBottom = Color.FromArgb(21, 23, 26);
        public static Color Side = Color.FromArgb(23, 25, 27);
        public static Color Panel = Color.FromArgb(32, 35, 38);
        public static Color Panel2 = Color.FromArgb(44, 47, 51);
        public static Color Line = Color.FromArgb(48, 51, 56);
        public static Color Txt = Color.FromArgb(226, 228, 230);
        public static Color Dim = Color.FromArgb(154, 160, 166);
        public static Color Accent = Color.FromArgb(217, 220, 224);
        public static Color AccentDark = Color.FromArgb(45, 48, 52);
        public static Color Gold = Color.FromArgb(232, 192, 112);
        public static Color Red = Color.FromArgb(240, 113, 120);
        public static Color Orange = Color.FromArgb(230, 170, 110);

        public static void Apply(AppSettings s)
        {
            BgBottom = ParseHex(s.BgBottom, Color.FromArgb(21, 23, 26));
            BgTop = ParseHex(s.BgTop, Color.FromArgb(32, 35, 38));
            Accent = ParseHex(s.Accent, Color.FromArgb(217, 220, 224));
            Bg = BgBottom;
            Side = Darken(BgBottom, 0.14f);
            Panel = Lighten(BgBottom, 0.07f);
            Panel2 = Lighten(BgBottom, 0.14f);
            Line = Lighten(BgBottom, 0.20f);
            AccentDark = Blend(BgBottom, Accent, 0.20f);
            Txt = Color.FromArgb(226, 228, 230);
            Dim = Color.FromArgb(154, 160, 166);
        }
        static Color ParseHex(string s, Color def)
        {
            try { if (!string.IsNullOrEmpty(s)) return ColorTranslator.FromHtml(s); } catch { }
            return def;
        }
        public static Color Blend(Color a, Color b, float k)
        {
            return Color.FromArgb(a.A, (int)(a.R + (b.R - a.R) * k), (int)(a.G + (b.G - a.G) * k), (int)(a.B + (b.B - a.B) * k));
        }

        public static Color Lighten(Color c, float k)
        {
            return Color.FromArgb(c.A, Math.Min(255, (int)(c.R + (255 - c.R) * k)), Math.Min(255, (int)(c.G + (255 - c.G) * k)), Math.Min(255, (int)(c.B + (255 - c.B) * k)));
        }
        public static Color Darken(Color c, float k)
        {
            return Color.FromArgb(c.A, Math.Max(0, (int)(c.R * (1 - k))), Math.Max(0, (int)(c.G * (1 - k))), Math.Max(0, (int)(c.B * (1 - k))));
        }
        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Max(2, rad * 2);
            if (d > r.Width) d = Math.Max(2, r.Width);
            if (d > r.Height) d = Math.Max(2, r.Height);
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        public static GraphicsPath Round(RectangleF r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Max(2, rad);
            if (d > r.Width) d = (int)Math.Max(2, r.Width);
            if (d > r.Height) d = (int)Math.Max(2, r.Height);
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    class FlatButton : Button
    {
        public Color Bg = Theme.Panel;
        public Color Hover = Theme.Panel2;
        public Color Fore = Theme.Txt;
        public Color Border = Theme.Line;
        public bool Primary = false;
        public int Radius = 6;
        bool hov, dwn;
        public FlatButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Fore;
            TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hov = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hov = false; dwn = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { dwn = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dwn = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color baseC = !Enabled ? Color.FromArgb(27, 29, 31) : Bg;
            Color c = !Enabled ? baseC : (dwn ? Theme.Darken(baseC, 0.14f) : (hov ? Theme.Lighten(baseC, 0.12f) : baseC));
            Rectangle rr = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(rr, Radius))
            {
                using (SolidBrush b = new SolidBrush(c)) g.FillPath(b, p);
                Color bc = Primary ? Theme.Accent : Border;
                int ba = Enabled ? (hov || dwn ? 210 : 120) : 60;
                using (Pen bp = new Pen(Color.FromArgb(ba, bc.R, bc.G, bc.B), 1f)) g.DrawPath(bp, p);
                if (!dwn)
                {
                    using (Pen shine = new Pen(Color.FromArgb(28, 255, 255, 255), 1f))
                    {
                        GraphicsPath top = new GraphicsPath();
                        top.AddArc(rr.X + 1, rr.Y + 1, Radius * 2, Radius * 2, 180, 90);
                        top.AddLine(rr.X + Radius, rr.Y + 1, rr.Right - Radius, rr.Y + 1);
                        top.AddArc(rr.Right - Radius * 2 - 1, rr.Y + 1, Radius * 2, Radius * 2, 270, 90);
                        g.DrawPath(shine, top);
                    }
                }
            }
            Color fc = Enabled ? (Primary ? Theme.Accent : Fore) : Color.FromArgb(110, 116, 124);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height - 1), fc,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
    }
    class NavButton : Button
    {
        public bool Active;
        public string Sub = "";
        bool hov;
        public NavButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hov = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hov = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg = Active ? Theme.AccentDark : (hov ? Theme.Panel : Theme.Side);
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 6))
            using (SolidBrush b = new SolidBrush(bg)) g.FillPath(b, p);
            if (Active) using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, 0, 7, 2, Height - 14);
            Color fg = Active ? Theme.Accent : (hov ? Theme.Txt : Theme.Dim);
            DrawIcon(g, Active ? Theme.Accent : (hov ? Theme.Txt : Theme.Dim), 14, Height / 2f - 7f, 14f);
            TextRenderer.DrawText(g, Sub, new Font("Segoe UI", 8.75f, Active ? FontStyle.Bold : FontStyle.Regular),
                new Rectangle(34, 0, Width - 40, Height), fg, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        void DrawIcon(Graphics g, Color c, float x, float y, float s)
        {
            using (Pen p = new Pen(c, 1.5f))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                if (Sub == "3D 依赖树")
                {
                    g.DrawLine(p, x + s / 2, y + s - 3, x + s / 2, y + s / 2);
                    g.DrawLine(p, x + s / 2, y + s / 2, x + 3, y + s / 3);
                    g.DrawLine(p, x + s / 2, y + s / 2, x + s - 3, y + s / 3);
                    using (SolidBrush b = new SolidBrush(c))
                    {
                        g.FillEllipse(b, x + s / 2 - 2, y + s - 5, 4, 4);
                        g.FillEllipse(b, x + 1.5f, y + s / 3 - 2.5f, 4, 4);
                        g.FillEllipse(b, x + s - 5.5f, y + s / 3 - 2.5f, 4, 4);
                    }
                }
                else if (Sub == "应用列表") { for (int i = 0; i < 3; i++) g.DrawLine(p, x + 3, y + 4 + i * 4.5f, x + s - 3, y + 4 + i * 4.5f); }
                else if (Sub == "下载目录")
                {
                    g.DrawLine(p, x + s / 2, y + 1, x + s / 2, y + s - 6);
                    g.DrawLine(p, x + s / 2, y + s - 6, x + s / 2 - 4, y + s - 10);
                    g.DrawLine(p, x + s / 2, y + s - 6, x + s / 2 + 4, y + s - 10);
                    g.DrawLine(p, x + 2, y + s - 2, x + s - 2, y + s - 2);
                }
                else if (Sub == "设置")
                {
                    float cx = x + s / 2, cy = y + s / 2;
                    g.DrawEllipse(p, cx - 3.2f, cy - 3.2f, 6.4f, 6.4f);
                    for (int i = 0; i < 6; i++)
                    {
                        double a = i * Math.PI / 3.0;
                        g.DrawLine(p, cx + (float)Math.Cos(a) * 5f, cy + (float)Math.Sin(a) * 5f, cx + (float)Math.Cos(a) * 7.5f, cy + (float)Math.Sin(a) * 7.5f);
                    }
                }
                else { g.DrawEllipse(p, x + 2, y + 1, s - 7, s - 7); g.DrawLine(p, x + s - 6, y + s - 6, x + s - 1, y + s - 1); }
            }
        }
    }

    class Sidebar : Panel
    {
        public Sidebar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Side);
            using (SolidBrush b = new SolidBrush(Theme.Line)) g.FillRectangle(b, Width - 1, 0, 1, Height);

        }
    }

    class DBList : ListBox
    {
        public DBList() { DoubleBuffered = true; }
    }

    class DBLabel : Label
    {
        public DBLabel() { DoubleBuffered = true; }
    }

    class ListRow
    {
        public string name = "", path = "", time = "", type = "";
        public long size = 0;
        public bool isDir = false;
        public Node node;
    }

    class AppItem
    {
        public string Name = "";
        public string Exe = "";
        public Bitmap Icon = null;
    }

    class MainForm : Form
    {
        Sidebar side;
        NavButton[] nav = new NavButton[6];
        Panel header, bottom, bottomInfo, colHeader, rightPan;
        Label pageTitle, lblStatus, lblPath, lblQueue, lblPluginInfo;
        TextBox txtSearch, txtManual, txtPath, txtFind;
        FlatButton btnScan, btnRotate, btnAddPath, btnQueue, btnDep, btnClear, btnVerify, btnDelete, btnUp, btnGo, btnRefresh, btnReveal, btnZoomIn, btnZoomOut, btnZoomReset, btnAllDisk, btnFind, btnShared, btnPluginRun, btnPluginToggle, btnPluginDir, btnPluginRefresh;
        AppSettings cfg;
        TreeCanvas2 canvas;
        RichTextBox rtbDetail;
        ListBox lstApps, lstDownloads, lstOnline, lstFiles, lstPlugins;
        List<FlatButton> driveBtns = new List<FlatButton>();
        List<AppItem> apps = new List<AppItem>();
        List<PluginItem> pluginItems = new List<PluginItem>();
        List<Dictionary<string, object>> downloads = new List<Dictionary<string, object>>();
        List<string> onlineUrls = new List<string>();
        List<ListRow> rows = new List<ListRow>();
        List<Node> queue = new List<Node>();
        Node browseNode;
        Label hoverTip;
        int hoverSeq = 0;
        string hoverKey = "";
        string browsePath = "";
        int page = 0, hovRow = -1;
        bool busy = false;
        float ui = 1f;
        int S(int v) { return (int)(v * ui); }

        public MainForm(string kw)
        {
            cfg = SettingsStore.Load();
            Theme.Apply(cfg);
            Text = "NukeTree · 3D 应用清理";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            using (Graphics dg0 = CreateGraphics()) ui = dg0.DpiX / 96f;
            ClientSize = new Size((int)(1520 * ui), (int)(1040 * ui));
            MinimumSize = new Size((int)(1180 * ui), (int)(820 * ui));
            DoubleBuffered = true;
            BackColor = Theme.Bg;
            ForeColor = Theme.Txt;
            Font = new Font("Segoe UI", 9f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            side = new Sidebar();
            Controls.Add(side);
            string[] names = { "3D 依赖树", "应用列表", "下载目录", "在线查依赖", "插件", "设置" };
            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                nav[i] = new NavButton();
                nav[i].Sub = names[i];
                nav[i].Click += delegate
                {
                    if (idx == 5) ShowSettings();
                    else if (idx == 4) { ShowPage(4); RefreshPlugins(); }
                    else if (idx == 2) LoadDownloads();
                    else if (idx == 3) LoadOnline(canvas.selected != null ? canvas.selected.name : txtSearch.Text);
                    else ShowPage(idx);
                };
                Controls.Add(nav[i]);
            }
            txtManual = MakeTextBox();
            Controls.Add(txtManual);
            btnAddPath = MakeButton("＋", Theme.Dim, Theme.Panel2);
            btnAddPath.Click += delegate { AddPath(txtManual.Text); };
            Controls.Add(btnAddPath);
            lblQueue = new Label(); lblQueue.ForeColor = Theme.Dim; lblQueue.AutoSize = true;
            Controls.Add(lblQueue);
            lblStatus = new Label(); lblStatus.ForeColor = Theme.Dim; lblStatus.AutoSize = true; lblStatus.Text = "就绪";
            Controls.Add(lblStatus);
            btnVerify = MakeButton("核验", Theme.Txt, Theme.Panel);
            btnVerify.Click += delegate { DoVerify(); };
            btnClear = MakeButton("清空", Theme.Dim, Theme.Panel);
            btnClear.Click += delegate { queue.Clear(); UpdateQueueLabel(); };
            btnDelete = MakeButton("删除", Color.FromArgb(255, 138, 143), Color.FromArgb(52, 24, 28));
            btnDelete.Border = Color.FromArgb(128, 64, 70);
            btnDelete.Fore = Color.FromArgb(255, 154, 160);
            btnDelete.Click += delegate { DoDelete(); };
            Controls.Add(btnVerify); Controls.Add(btnClear); Controls.Add(btnDelete);
            ToolTip tips = new ToolTip();
            tips.SetToolTip(btnVerify, "核验：残留文件 + 进程内存");
            tips.SetToolTip(btnDelete, "删除选中项（自动备份）");
            tips.SetToolTip(btnClear, "清空删除队列");
            tips.SetToolTip(btnAddPath, "把路径加入 3D 树");

            header = new Panel();
            header.Paint += delegate (object s, PaintEventArgs e) { using (SolidBrush b = new SolidBrush(Theme.Line)) e.Graphics.FillRectangle(b, 0, header.Height - 1, header.Width, 1); };
            Controls.Add(header);
            txtSearch = MakeTextBox();
            header.Controls.Add(txtSearch);
            txtSearch.KeyDown += delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) StartScan(txtSearch.Text); };
            btnScan = MakeButton("扫描", Theme.Accent, Theme.AccentDark);
            btnScan.Primary = true;
            btnScan.Click += delegate { StartScan(txtSearch.Text); };
            header.Controls.Add(btnScan);
            btnAllDisk = MakeButton("全盘", Theme.Dim, Theme.Panel);
            btnAllDisk.Click += delegate { LoadAllDrives(); };
            header.Controls.Add(btnAllDisk);
            btnShared = MakeButton("共用依赖", Theme.Dim, Theme.Panel);
            btnShared.Click += delegate { DoShared(); };
            header.Controls.Add(btnShared);
            btnRotate = MakeButton("自动旋转", Theme.Dim, Theme.Panel);
            btnRotate.Click += delegate
            {
                canvas.autoRotate = !canvas.autoRotate;
                btnRotate.Text = canvas.autoRotate ? "自动旋转" : "已暂停";
                btnRotate.Fore = canvas.autoRotate ? Theme.Dim : Theme.Accent;
                btnRotate.Invalidate();
            };
            header.Controls.Add(btnRotate);
            btnZoomIn = MakeButton("＋", Theme.Dim, Theme.Panel);
            btnZoomIn.Click += delegate { canvas.ZoomBy(1.25); btnRotate.Text = "自动旋转"; btnRotate.Fore = canvas.autoRotate ? Theme.Dim : Theme.Accent; };
            header.Controls.Add(btnZoomIn);
            btnZoomOut = MakeButton("－", Theme.Dim, Theme.Panel);
            btnZoomOut.Click += delegate { canvas.ZoomBy(0.8); };
            header.Controls.Add(btnZoomOut);
            btnZoomReset = MakeButton("复位", Theme.Dim, Theme.Panel);
            btnZoomReset.Click += delegate { canvas.ResetView(); };
            header.Controls.Add(btnZoomReset);
            txtFind = MakeTextBox();
            header.Controls.Add(txtFind);
            txtFind.KeyDown += delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) DoFind(); };
            btnFind = MakeButton("查找", Theme.Dim, Theme.Panel);
            btnFind.Click += delegate { DoFind(); };
            header.Controls.Add(btnFind);
            pageTitle = new Label();
            pageTitle.ForeColor = Theme.Txt;
            pageTitle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            pageTitle.BackColor = Color.Transparent;
            pageTitle.AutoSize = true;
            header.Controls.Add(pageTitle);

            canvas = new TreeCanvas2();
            Controls.Add(canvas);
            canvas.SelectionChanged += delegate { OnNodeSelected(canvas.selected); };
            canvas.NodeDoubleClicked += delegate (Node nd) { ExpandNode(nd); };
            canvas.AllowDrop = true;
            canvas.DragEnter += delegate (object s, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None;
            };
            canvas.DragDrop += delegate (object s, DragEventArgs e)
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                AddDroppedPaths((string[])e.Data.GetData(DataFormats.FileDrop));
            };
            AllowDrop = true;
            DragEnter += delegate (object s, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None;
            };
            DragDrop += delegate (object s, DragEventArgs e)
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                string[] fs = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddDroppedPaths(fs);
            };

            bottom = new Panel();
            bottom.Paint += delegate (object s, PaintEventArgs e) { using (SolidBrush b = new SolidBrush(Theme.Line)) e.Graphics.FillRectangle(b, 0, 0, bottom.Width, 1); };
            Controls.Add(bottom);
            lblPath = new Label();
            lblPath.ForeColor = Theme.Dim;
            bottom.Controls.Add(lblPath);
            btnUp = MakeButton("↑", Theme.Dim, Theme.Panel);
            btnUp.Click += delegate { GoUp(); };
            bottom.Controls.Add(btnUp);
            btnRefresh = MakeButton("刷新", Theme.Dim, Theme.Panel);
            btnRefresh.Click += delegate { Browse(browsePath); };
            bottom.Controls.Add(btnRefresh);
            btnReveal = MakeButton("资源管理器", Theme.Dim, Theme.Panel);
            btnReveal.Click += delegate { Reveal(); };
            bottom.Controls.Add(btnReveal);
            txtPath = MakeTextBox();
            bottom.Controls.Add(txtPath);
            txtPath.KeyDown += delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) Browse(txtPath.Text.Trim()); };
            btnGo = MakeButton("前往", Theme.Dim, Theme.Panel);
            btnGo.Click += delegate { Browse(txtPath.Text.Trim()); };
            bottom.Controls.Add(btnGo);
            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                try
                {
                    if (!di.IsReady) continue;
                    string label = di.Name.TrimEnd('\\');
                    FlatButton db = MakeButton(label, Theme.Dim, Theme.Panel);
                    db.Click += delegate { Browse(di.Name); };
                    bottom.Controls.Add(db);
                    driveBtns.Add(db);
                }
                catch { }
            }
            colHeader = new Panel();
            colHeader.Paint += delegate (object s, PaintEventArgs e) { DrawColumnHeader(e.Graphics); };
            bottom.Controls.Add(colHeader);
            lstFiles = new DBList();
            lstFiles.BackColor = Theme.Bg;
            lstFiles.ForeColor = Theme.Txt;
            lstFiles.BorderStyle = BorderStyle.None;
            lstFiles.DrawMode = DrawMode.OwnerDrawFixed;
            lstFiles.ItemHeight = 24;
            lstFiles.DrawItem += DrawFileRow;
            lstFiles.DoubleClick += delegate { OpenRow(); };
            lstFiles.MouseMove += delegate {
                int i = lstFiles.IndexFromPoint(lstFiles.PointToClient(Cursor.Position));
                if (i != hovRow)
                {
                    int old = hovRow; hovRow = i;
                    if (old >= 0 && old < lstFiles.Items.Count) lstFiles.Invalidate(lstFiles.GetItemRectangle(old));
                    if (i >= 0) lstFiles.Invalidate(lstFiles.GetItemRectangle(i));
                    if (i >= 0 && i < rows.Count) ShowHoverRow(rows[i], Cursor.Position); else HideHover();
                }
            };
            lstFiles.MouseLeave += delegate { HideHover(); };
            lstFiles.MouseLeave += delegate { hovRow = -1; lstFiles.Invalidate(); };
            bottom.Controls.Add(lstFiles);
            bottomInfo = new Panel();
            bottomInfo.Paint += delegate (object s, PaintEventArgs e) { using (SolidBrush b = new SolidBrush(Theme.Line)) e.Graphics.FillRectangle(b, 0, 0, 1, bottomInfo.Height); };
            bottom.Controls.Add(bottomInfo);
            rtbDetail = new RichTextBox();
            rtbDetail.BackColor = Theme.Panel;
            rtbDetail.ForeColor = Theme.Txt;
            rtbDetail.BorderStyle = BorderStyle.None;
            rtbDetail.ReadOnly = true;
            rtbDetail.ScrollBars = RichTextBoxScrollBars.Vertical;
            bottomInfo.Controls.Add(rtbDetail);
            btnQueue = MakeButton("加入删除队列", Theme.Accent, Theme.AccentDark);
            btnQueue.Primary = true;
            btnQueue.Click += delegate { ToggleQueue(canvas.selected); };
            bottomInfo.Controls.Add(btnQueue);
            btnDep = MakeButton("查依赖 / 在线", Theme.Dim, Theme.Panel);
            btnDep.Click += delegate { LoadOnline(canvas.selected != null ? canvas.selected.name : txtSearch.Text); };
            bottomInfo.Controls.Add(btnDep);

            lstApps = MakeList();
            lstApps.DrawMode = DrawMode.OwnerDrawFixed;
            lstApps.ItemHeight = 46;
            lstApps.DrawItem += DrawAppRow;
            lstApps.DoubleClick += delegate { AppItem a = lstApps.SelectedItem as AppItem; if (a != null) { txtSearch.Text = a.Name; StartScan(a.Name); } };
            lstApps.MouseMove += delegate { int i = lstApps.IndexFromPoint(lstApps.PointToClient(Cursor.Position)); if (i != hovRow) { int old = hovRow; hovRow = i; if (old >= 0 && old < lstApps.Items.Count) lstApps.Invalidate(lstApps.GetItemRectangle(old)); if (i >= 0) lstApps.Invalidate(lstApps.GetItemRectangle(i)); } };
            Controls.Add(lstApps);
            lstDownloads = MakeList();
            lstDownloads.DrawMode = DrawMode.OwnerDrawFixed;
            lstDownloads.ItemHeight = 46;
            lstDownloads.DrawItem += DrawDownloadRow;
            lstDownloads.DoubleClick += delegate { int i = lstDownloads.SelectedIndex; if (i >= 0 && i < downloads.Count) AddPath(Convert.ToString(downloads[i]["path"])); };
            Controls.Add(lstDownloads);
            lstOnline = MakeList();
            lstOnline.DrawMode = DrawMode.OwnerDrawFixed;
            lstOnline.ItemHeight = 32;
            lstOnline.DrawItem += DrawPlainRow;
            lstOnline.DoubleClick += delegate { int i = lstOnline.SelectedIndex; if (i >= 0 && i < onlineUrls.Count) { try { System.Diagnostics.Process.Start(onlineUrls[i]); } catch { } } };
            Controls.Add(lstOnline);

            // 插件页
            lblPluginInfo = new Label();
            lblPluginInfo.ForeColor = Theme.Dim;
            lblPluginInfo.AutoSize = false;
            lblPluginInfo.Text = "把插件放到 plugins\\<名字>\\ 下（plugin.json + 脚本），刷新即可加载";
            Controls.Add(lblPluginInfo);
            lstPlugins = MakeList();
            lstPlugins.DrawMode = DrawMode.OwnerDrawFixed;
            lstPlugins.ItemHeight = 58;
            lstPlugins.DrawItem += DrawPluginRow;
            lstPlugins.DoubleClick += delegate { RunSelectedPlugin(); };
            Controls.Add(lstPlugins);
            btnPluginRun = MakeButton("运行", Theme.Accent, Theme.AccentDark);
            btnPluginRun.Primary = true;
            btnPluginRun.Click += delegate { RunSelectedPlugin(); };
            Controls.Add(btnPluginRun);
            btnPluginToggle = MakeButton("启用/停用", Theme.Dim, Theme.Panel);
            btnPluginToggle.Click += delegate { TogglePlugin(); };
            Controls.Add(btnPluginToggle);
            btnPluginRefresh = MakeButton("刷新", Theme.Dim, Theme.Panel);
            btnPluginRefresh.Click += delegate { RefreshPlugins(); };
            Controls.Add(btnPluginRefresh);
            btnPluginDir = MakeButton("打开插件目录", Theme.Dim, Theme.Panel);
            btnPluginDir.Click += delegate { OpenPluginDir(); };
            Controls.Add(btnPluginDir);

            // 布局重排：文件浏览器放右侧，详情放底部
            rightPan = new Panel();
            rightPan.Paint += delegate (object s, PaintEventArgs e) { using (SolidBrush b = new SolidBrush(Theme.Line)) e.Graphics.FillRectangle(b, 0, 0, 1, rightPan.Height); };
            Controls.Add(rightPan);
            Control[] fileCtrls = new Control[] { btnUp, btnRefresh, btnReveal, txtPath, btnGo, lblPath, colHeader, lstFiles };
            foreach (Control c in fileCtrls) rightPan.Controls.Add(c);
            foreach (FlatButton db in driveBtns) rightPan.Controls.Add(db);
            bottomInfo.Visible = false;
            foreach (Control c in new Control[] { rtbDetail, btnQueue, btnDep }) bottom.Controls.Add(c);

            hoverTip = new DBLabel();
            hoverTip.BackColor = Theme.Panel;
            hoverTip.ForeColor = Theme.Txt;
            hoverTip.AutoSize = false;
            hoverTip.Text = "悬停文件查看作用";
            hoverTip.Padding = new Padding(8, 6, 8, 6);
            rightPan.Controls.Add(hoverTip);

            canvas.HoverChanged += delegate (Node nd, Point scr) { ShowHoverNode(nd, scr); };
            canvas.HoverEnded += delegate { HideHover(); };

            Load += delegate
            {
                DoLayout();
                Program.Log("ui=" + ui + " client=" + ClientSize.Width + "x" + ClientSize.Height);
                ShowPage(0);
                if (kw == "__settings") { BeginInvoke((MethodInvoker)delegate { ShowSettings(); }); kw = ""; }
                if (kw == "__alldisk") { BeginInvoke((MethodInvoker)delegate { LoadAllDrives(); }); kw = ""; }
                if (kw == "__apps") { BeginInvoke((MethodInvoker)delegate { ShowPage(1); }); kw = ""; }
                if (kw == "__downloads") { BeginInvoke((MethodInvoker)delegate { LoadDownloads(); }); kw = ""; }
                if (kw == "__plugins") { BeginInvoke((MethodInvoker)delegate { ShowPage(4); RefreshPlugins(); }); kw = ""; }
                LoadAppsAsync();
                if (!string.IsNullOrEmpty(kw)) { txtSearch.Text = kw; StartScan(kw); }
            };
            Resize += delegate { DoLayout(); };
            DoLayout();
            side.SendToBack();
        }

        TextBox MakeTextBox()
        {
            TextBox t = new TextBox();
            t.BackColor = Theme.Panel;
            t.ForeColor = Theme.Txt;
            t.BorderStyle = BorderStyle.FixedSingle;
            return t;
        }
        FlatButton MakeButton(string text, Color fore, Color bg)
        {
            FlatButton b = new FlatButton();
            b.Text = text; b.Fore = fore; b.Bg = bg; b.Hover = Theme.Panel2;
            b.Font = new Font("Segoe UI", 8.25f);
            return b;
        }
        ListBox MakeList()
        {
            ListBox l = new DBList();
            l.BackColor = Theme.Bg;
            l.ForeColor = Theme.Txt;
            l.BorderStyle = BorderStyle.None;
            return l;
        }

        void DoLayout()
        {
            int sw = S(180), hh = S(44), bh = S(82), rw = S(360);
            int W = ClientSize.Width, H = ClientSize.Height;
            if (W < S(900) || H < S(600)) return;
            side.SetBounds(0, 0, sw, H);
            for (int i = 0; i < 5; i++) nav[i].SetBounds(S(8), S(14) + i * S(34), sw - S(16), S(30));
            nav[5].SetBounds(S(6), H - S(34), sw - S(12), S(28));

            header.SetBounds(sw, 0, W - sw, hh);
            txtSearch.SetBounds(S(16), S(9), S(260), S(24));
            btnScan.SetBounds(S(282), S(8), S(60), S(26));
            btnRotate.SetBounds(S(348), S(8), S(80), S(26));
            btnZoomIn.SetBounds(S(434), S(8), S(24), S(26));
            btnZoomOut.SetBounds(S(462), S(8), S(24), S(26));
            btnZoomReset.SetBounds(S(490), S(8), S(40), S(26));
            btnAllDisk.SetBounds(S(536), S(8), S(52), S(26));
            btnShared.SetBounds(S(1132), S(8), S(80), S(26));
            pageTitle.SetBounds(S(596), S(11), S(310), S(22));
            txtFind.SetBounds(S(918), S(9), S(150), S(24));
            btnFind.SetBounds(S(1074), S(8), S(50), S(26));

            canvas.SetBounds(sw, hh, W - sw - rw, H - hh - bh);

            rightPan.SetBounds(W - rw, hh, rw, H - hh);
            btnUp.SetBounds(S(8), S(6), S(26), S(24));
            btnRefresh.SetBounds(S(38), S(6), S(46), S(24));
            btnReveal.SetBounds(S(88), S(6), S(76), S(24));
            int dx = S(170);
            foreach (FlatButton db in driveBtns) { db.SetBounds(dx, S(6), S(30), S(24)); dx += S(32); }
            txtPath.SetBounds(S(8), S(34), rw - S(16) - S(56), S(22));
            btnGo.SetBounds(rw - S(62), S(34), S(54), S(22));
            lblPath.SetBounds(S(8), S(60), rw - S(12), S(14));
            colHeader.SetBounds(S(8), S(78), rw - S(16), S(18));
            lstFiles.SetBounds(S(8), S(98), rw - S(16), H - hh - S(98) - S(110));
            hoverTip.SetBounds(S(8), H - hh - S(104), rw - S(16), S(96));

            bottom.SetBounds(sw, H - bh, W - sw - rw, bh);
            rtbDetail.SetBounds(S(10), S(6), W - sw - rw - S(230), bh - S(12));
            btnQueue.SetBounds(W - sw - rw - S(220), S(8), S(100), S(24));
            btnDep.SetBounds(W - sw - rw - S(220), S(38), S(100), S(24));

            int baseY = H - S(218);
            lblStatus.SetBounds(S(12), baseY, sw - S(20), S(14));
            lblQueue.SetBounds(S(12), baseY + S(18), sw - S(20), S(14));
            txtManual.SetBounds(S(12), baseY + S(44), sw - S(20) - S(30), S(24));
            btnAddPath.SetBounds(sw - S(42), baseY + S(44), S(26), S(24));
            int bw = (sw - S(24) - S(8)) / 3;
            btnVerify.SetBounds(S(12), baseY + S(78), bw, S(24));
            btnDelete.SetBounds(S(12) + bw + S(4), baseY + S(78), bw, S(24));
            btnClear.SetBounds(S(12) + (bw + S(4)) * 2, baseY + S(78), bw, S(24));

            lstApps.SetBounds(sw + S(16), hh + S(10), W - sw - S(32), H - hh - S(20));
            lstDownloads.SetBounds(sw + S(16), hh + S(10), W - sw - S(32), H - hh - S(20));
            lstOnline.SetBounds(sw + S(16), hh + S(10), W - sw - S(32), H - hh - S(20));
            lblPluginInfo.SetBounds(sw + S(20), hh + S(10), W - sw - S(40), S(22));
            lstPlugins.SetBounds(sw + S(20), hh + S(40), W - sw - S(40), H - hh - S(100));
            btnPluginRun.SetBounds(sw + S(20), H - S(44), S(90), S(28));
            btnPluginToggle.SetBounds(sw + S(116), H - S(44), S(100), S(28));
            btnPluginRefresh.SetBounds(sw + S(222), H - S(44), S(80), S(28));
            btnPluginDir.SetBounds(sw + S(308), H - S(44), S(120), S(28));
        }
        void ShowPage(int i)
        {
            page = i;
            for (int k = 0; k < 5; k++) { nav[k].Active = (k == i); nav[k].Invalidate(); }
            canvas.Visible = (i == 0);
            bottom.Visible = (i == 0);
            if (rightPan != null) rightPan.Visible = (i == 0);
            lstApps.Visible = (i == 1);
            lstDownloads.Visible = (i == 2);
            lstOnline.Visible = (i == 3);
            if (lstPlugins != null) { lstPlugins.Visible = (i == 4); btnPluginRun.Visible = (i == 4); btnPluginToggle.Visible = (i == 4); btnPluginDir.Visible = (i == 4); btnPluginRefresh.Visible = (i == 4); lblPluginInfo.Visible = (i == 4); }
            if (txtFind != null) { txtFind.Visible = (i == 0); btnFind.Visible = (i == 0); btnShared.Visible = (i == 0); }
            if (i == 0) pageTitle.Text = "3D 依赖树";
            else if (i == 1) pageTitle.Text = "应用列表 · " + apps.Count + " 个应用 · 双击扫描";
            else if (i == 2) pageTitle.Text = "下载目录 · " + downloads.Count + " 项 · 双击加入树";
            else if (i == 3) pageTitle.Text = "在线查依赖 · 双击打开";
            else pageTitle.Text = "插件 · 扩展功能";
            DoLayout();
        }

        // ---- 底部文件浏览器 ----
        void DrawColumnHeader(Graphics g)
        {
            g.Clear(Theme.Bg);
            string[] heads = { "名称", "大小", "类型", "修改时间" };
            int w = colHeader.Width;
            int[] xs = { S(30), (int)(w * 0.40), (int)(w * 0.52), (int)(w * 0.73) };
            using (Font f = new Font("Segoe UI", 8.5f))
                for (int i = 0; i < heads.Length; i++)
                    g.DrawString(heads[i], f, new SolidBrush(Theme.Dim), xs[i], S(4));
            using (SolidBrush b = new SolidBrush(Theme.Line)) g.FillRectangle(b, 0, colHeader.Height - 1, w, 1);
        }
        void DrawFileRow(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= rows.Count) return;
            ListRow r = rows[e.Index];
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool hov = sel || e.Index == hovRow;
            using (SolidBrush b = new SolidBrush(sel ? Theme.Panel2 : (hov ? Theme.Panel : Theme.Bg))) e.Graphics.FillRectangle(b, e.Bounds);
            if (sel) using (SolidBrush b = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(b, e.Bounds.X + 4, e.Bounds.Y + 6, 2, e.Bounds.Height - 12);
            int w = e.Bounds.Width;
            int[] xs = { e.Bounds.X + S(30), (int)(e.Bounds.X + w * 0.40), (int)(e.Bounds.X + w * 0.52), (int)(e.Bounds.X + w * 0.73) };
            DrawFileIcon(e.Graphics, e.Bounds.X + S(10), e.Bounds.Y + e.Bounds.Height / 2 - S(8), S(16), r);
            TextRenderer.DrawText(e.Graphics, r.name, Font, new Rectangle(xs[0], e.Bounds.Y, xs[1] - xs[0] - S(10), e.Bounds.Height),
                sel ? Theme.Txt : Color.FromArgb(214, 220, 228), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, r.isDir ? "" : FmtSize(r.size), Font, new Rectangle(xs[1], e.Bounds.Y, xs[2] - xs[1] - S(8), e.Bounds.Height),
                Theme.Dim, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, r.type, Font, new Rectangle(xs[2], e.Bounds.Y, xs[3] - xs[2] - S(8), e.Bounds.Height),
                Theme.Dim, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            string shortTime = r.time.Length >= 16 ? r.time.Substring(5, 11) : r.time;
            TextRenderer.DrawText(e.Graphics, shortTime, Font, new Rectangle(xs[3], e.Bounds.Y, e.Bounds.Right - xs[3] - S(8), e.Bounds.Height),
                Color.FromArgb(110, 119, 133), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(22, 38, 43, 53))) e.Graphics.FillRectangle(b, e.Bounds.X + S(8), e.Bounds.Bottom - 1, e.Bounds.Width - S(16), 1);
        }
        void DrawFileIcon(Graphics g, float x, float y, float s, ListRow r)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (r.isDir)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(120, 180, 255))) g.FillPath(b, FolderPath(x, y + 1, s));
                using (Pen p = new Pen(Color.FromArgb(170, 210, 255), 0.8f)) g.DrawPath(p, FolderPath(x, y + 1, s));
            }
            else
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 168, 180))) g.FillRectangle(b, x + 2, y + 1, s - 4, s - 2);
                using (Pen p = new Pen(Theme.Bg, 1f))
                {
                    g.DrawLine(p, x + 4, y + 5, x + s - 6, y + 5);
                    g.DrawLine(p, x + 4, y + 8, x + s - 7, y + 8);
                }
            }
        }
        GraphicsPath FolderPath(float x, float y, float s)
        {
            GraphicsPath p = new GraphicsPath();
            p.AddLine(x, y + s * 0.22f, x + s * 0.40f, y + s * 0.22f);
            p.AddLine(x + s * 0.48f, y + s * 0.34f, x + s, y + s * 0.34f);
            p.AddLine(x + s, y + s, x, y + s);
            p.CloseFigure();
            return p;
        }
        void DrawDownloadRow(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= downloads.Count) return;
            Dictionary<string, object> o = downloads[e.Index];
            string name = Convert.ToString(o["name"]);
            string full = o.ContainsKey("path") ? Convert.ToString(o["path"]) : "";
            string dir = "";
            try { dir = System.IO.Path.GetDirectoryName(full); } catch { }
            long sz = 0; try { sz = (long)Convert.ToDouble(o["size"]); } catch { }
            string time = o.ContainsKey("time") ? Convert.ToString(o["time"]) : "";
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool hov = (e.Index == hovRow);
            Rectangle b = e.Bounds;
            using (SolidBrush bg = new SolidBrush(sel ? Theme.Panel2 : (hov ? Theme.Panel : Theme.Bg))) e.Graphics.FillRectangle(bg, b);

            int s = 32;
            Rectangle ir = new Rectangle(b.X + S(16), b.Y + (b.Height - s) / 2, s, s);
            Color ic = Color.FromArgb(120, 150, 178);
            string ext = "";
            try { ext = System.IO.Path.GetExtension(name).TrimStart('.').ToUpper(); } catch { }
            if (ext == "EXE" || ext == "MSI") ic = Color.FromArgb(200, 176, 128);
            else if (ext == "ZIP" || ext == "7Z" || ext == "RAR") ic = Color.FromArgb(150, 150, 190);
            else if (ext == "PNG" || ext == "JPG" || ext == "JPEG" || ext == "WEBP" || ext == "GIF") ic = Color.FromArgb(150, 190, 160);
            using (GraphicsPath gp = Theme.Round(ir, S(7)))
            using (SolidBrush fb = new SolidBrush(Color.FromArgb(52, ic))) e.Graphics.FillPath(fb, gp);
            if (ext.Length == 0)
            {
                using (Pen dp = new Pen(ic, 1.2f * S(1)))
                {
                    RectangleF fr = new RectangleF(ir.X + s * 0.28f, ir.Y + s * 0.22f, s * 0.42f, s * 0.56f);
                    e.Graphics.DrawRectangle(dp, fr.X, fr.Y, fr.Width, fr.Height);
                    e.Graphics.DrawLine(dp, fr.X + 3, fr.Y + fr.Height * 0.45f, fr.Right - 3, fr.Y + fr.Height * 0.45f);
                    e.Graphics.DrawLine(dp, fr.X + 3, fr.Y + fr.Height * 0.72f, fr.Right - 3, fr.Y + fr.Height * 0.72f);
                }
            }
            else
            {
                using (Font ef = new Font("Segoe UI", 7.2f, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, ext.Length > 4 ? ext.Substring(0, 4) : ext, ef, ir, ic, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            int tx = ir.Right + S(14);
            using (Font nf = new Font("Segoe UI", 10.2f, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, name, nf, new Rectangle(tx, b.Y + S(6), b.Width - tx - S(120), S(22)),
                    sel ? Theme.Txt : Color.FromArgb(228, 232, 237), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            string meta = (dir ?? "");
            if (meta.Length > 78) meta = meta.Substring(0, 75) + "…";
            using (Font sf = new Font("Segoe UI", 8.4f))
                TextRenderer.DrawText(e.Graphics, meta, sf, new Rectangle(tx, b.Y + S(24), b.Width - tx - S(120), S(18)),
                    Theme.Dim, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            string szTxt = (sz > 0 ? FmtSize(sz) : "") + (string.IsNullOrEmpty(time) ? "" : ((sz > 0 ? "  ·  " : "") + time));
            using (Font zf = new Font("Segoe UI", 9f))
                TextRenderer.DrawText(e.Graphics, szTxt, zf,
                    new Rectangle(b.Right - S(220), b.Y, S(204), b.Height),
                    Color.FromArgb(180, 190, 200), TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (sel) using (SolidBrush sb = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(sb, b.X + S(6), b.Y + S(10), S(3), b.Height - S(20));
            using (SolidBrush ln = new SolidBrush(Color.FromArgb(18, 48, 52, 58))) e.Graphics.FillRectangle(ln, b.X + S(10), b.Bottom - 1, b.Width - S(20), 1);
        }
        void DrawAppRow(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstApps.Items.Count) return;
            AppItem a = lstApps.Items[e.Index] as AppItem;
            if (a == null) return;
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool hov = (e.Index == hovRow);
            Rectangle b = e.Bounds;
            using (SolidBrush bg = new SolidBrush(sel ? Theme.Panel2 : (hov ? Theme.Panel : Theme.Bg))) e.Graphics.FillRectangle(bg, b);

            int s = 34;
            Rectangle ir = new Rectangle(b.X + S(16), b.Y + (b.Height - s) / 2, s, s);
            if (a.Icon != null)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                e.Graphics.DrawImage(a.Icon, ir);
            }
            else
            {
                using (GraphicsPath gp = Theme.Round(ir, S(7)))
                using (SolidBrush fb = new SolidBrush(Theme.Panel2)) e.Graphics.FillPath(fb, gp);
                using (Font f2 = new Font("Segoe UI", 12f, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, a.Name.Length > 0 ? a.Name.Substring(0, 1).ToUpper() : "?", f2, ir, Theme.Dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            int tx = ir.Right + S(16);
            using (Font nf = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, a.Name, nf, new Rectangle(tx, b.Y + S(7), b.Width - tx - S(20), S(22)),
                    sel ? Theme.Txt : Color.FromArgb(228, 232, 237), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            string sub = a.Exe ?? "";
            if (sub.Length > 84) sub = sub.Substring(0, 81) + "…";
            using (Font sf = new Font("Segoe UI", 8.4f))
                TextRenderer.DrawText(e.Graphics, sub, sf, new Rectangle(tx, b.Y + S(24), b.Width - tx - S(20), S(18)),
                    Theme.Dim, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (sel) using (SolidBrush sb = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(sb, b.X + S(6), b.Y + S(10), S(3), b.Height - S(20));
            using (SolidBrush ln = new SolidBrush(Color.FromArgb(18, 48, 52, 58))) e.Graphics.FillRectangle(ln, b.X + S(10), b.Bottom - 1, b.Width - S(20), 1);
        }
        void DrawPlainRow(object sender, DrawItemEventArgs e)
        {
            ListBox l = (ListBox)sender;
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush b = new SolidBrush(sel ? Theme.Panel2 : (e.Index == hovRow ? Theme.Panel : Theme.Bg))) e.Graphics.FillRectangle(b, e.Bounds);
            if (sel) using (SolidBrush b = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(b, e.Bounds.X + 4, e.Bounds.Y + 6, 2, e.Bounds.Height - 12);
            TextRenderer.DrawText(e.Graphics, Convert.ToString(l.Items[e.Index]), l.Font,
                new Rectangle(e.Bounds.X + S(14), e.Bounds.Y, e.Bounds.Width - S(20), e.Bounds.Height),
                sel ? Theme.Txt : Color.FromArgb(210, 216, 226), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        void FillRowsFromNode(Node n)
        {
            rows.Clear();
            if (n != null)
            {
                foreach (Node c in n.children)
                {
                    ListRow r = new ListRow();
                    r.name = c.name; r.path = c.path; r.size = c.size; r.time = c.time;
                    r.isDir = (c.kind == "dir"); r.type = KindName(c.kind); r.node = c;
                    rows.Add(r);
                }
            }
            lstFiles.BeginUpdate();
            lstFiles.Items.Clear();
            foreach (ListRow r in rows) lstFiles.Items.Add(r.name);
            lstFiles.EndUpdate();
            lblPath.Text = browsePath.Length > 0 ? browsePath : (n != null ? ("选中节点: " + n.name) : "未选择");
            if (browsePath.Length > 0) txtPath.Text = browsePath;
        }
        void OnNodeSelected(Node n)
        {
            if (n == null) { ShowDetail(null); return; }
            browseNode = n;
            browsePath = (n.kind == "app") ? "" : (n.path != null ? n.path : "");
            FillRowsFromNode(n);
            ShowDetail(n);
        }
        void OpenRow()
        {
            int i = lstFiles.SelectedIndex;
            if (i < 0 || i >= rows.Count) return;
            ListRow r = rows[i];
            if (r.isDir && r.path.Length > 0) Browse(r.path);
            else if (r.path.Length > 0) System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + r.path + "\"");
        }
        void GoUp()
        {
            if (browsePath.Length == 0) return;
            string parent = Path.GetDirectoryName(browsePath.TrimEnd('\\'));
            if (!string.IsNullOrEmpty(parent)) Browse(parent);
        }
        void Reveal()
        {
            if (browsePath.Length > 0 && Directory.Exists(browsePath)) System.Diagnostics.Process.Start("explorer.exe", "\"" + browsePath + "\"");
            else if (browsePath.Length > 0 && File.Exists(browsePath)) System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + browsePath + "\"");
        }
        void Browse(string p)
        {
            p = (p == null ? "" : p.Trim().Trim('"'));
            if (p.Length == 0 || busy) return;
            if (!Directory.Exists(p) && !File.Exists(p)) { lblPath.Text = "路径不存在: " + p; return; }
            string target = Directory.Exists(p) ? p : Path.GetDirectoryName(p);
            browsePath = target;
            SetBusy(true, "读取目录…");
            string arg = target;
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/scan?path=" + Uri.EscapeDataString(arg), 60000);
                    Node root = ParseTree(json);
                    Node dirNode = (root != null && root.children.Count > 0) ? root.children[0] : null;
                    BeginInvoke((MethodInvoker)delegate
                    {
                        rows.Clear();
                        if (dirNode != null)
                        {
                            foreach (Node c in dirNode.children)
                            {
                                ListRow r = new ListRow();
                                r.name = c.name; r.path = c.path; r.size = c.size; r.time = c.time;
                                r.isDir = (c.kind == "dir"); r.type = KindName(c.kind); r.node = c;
                                rows.Add(r);
                            }
                        }
                        lstFiles.BeginUpdate(); lstFiles.Items.Clear();
                        foreach (ListRow r in rows) lstFiles.Items.Add(r.name);
                        lstFiles.EndUpdate();
                        lblPath.Text = target;
                        txtPath.Text = target;
                        browseNode = dirNode;
                        ShowDetail(dirNode);
                        SetBusy(false, "目录: " + rows.Count + " 项");
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "读取失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }

        void LoadAppsAsync()
        {
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/apps", 30000);
                    Dictionary<string, object> d = Json.Parse(json);
                    ArrayList arr = (ArrayList)d["apps"];
                    List<AppItem> list = new List<AppItem>();
                    foreach (object o in arr)
                    {
                        Dictionary<string, object> a = (Dictionary<string, object>)o;
                        AppItem it = new AppItem();
                        it.Name = Convert.ToString(a["name"]);
                        it.Exe = Convert.ToString(a["exe"]);
                        if (!string.IsNullOrEmpty(it.Exe) && File.Exists(it.Exe))
                        {
                            try
                            {
                                Icon ic = Icon.ExtractAssociatedIcon(it.Exe);
                                if (ic != null) { it.Icon = ic.ToBitmap(); ic.Dispose(); }
                            }
                            catch { }
                        }
                        list.Add(it);
                    }
                    list.Sort(delegate (AppItem x, AppItem y) { return string.Compare(x.Name, y.Name, StringComparison.CurrentCulture); });
                    apps = list;
                    BeginInvoke((MethodInvoker)delegate { RefreshAppList(); if (page == 1) pageTitle.Text = "应用列表 · " + apps.Count + " 个应用 · 双击扫描"; });
                }
                catch (Exception ex) { Program.Log("apps err " + ex.Message); }
            });
            t.IsBackground = true; t.Start();
        }
        void RefreshAppList()
        {
            lstApps.BeginUpdate();
            lstApps.Items.Clear();
            foreach (AppItem a in apps) lstApps.Items.Add(a);
            lstApps.EndUpdate();
        }
        void StartScan(string kw)
        {
            kw = (kw == null ? "" : kw.Trim());
            if (kw.Length == 0 || busy) return;
            ShowPage(0);
            SetBusy(true, "正在扫描「" + kw + "」… 首次约 10-20 秒");
            string urlKw = kw;
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/scan?kw=" + Uri.EscapeDataString(urlKw), 180000);
                    Node root = ParseTree(json);
                    BeginInvoke((MethodInvoker)delegate
                    {
                        canvas.SetTree(root);
                        queue.Clear(); UpdateQueueLabel(); ShowDetail(null);
                        if (root != null && root.children.Count > 0) OnNodeSelected(root.children[0]);
                        SetBusy(false, "扫描完成 · 节点 " + canvas.flat.Count + " 个");
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "扫描失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }
        void AddPath(string p)
        {
            p = (p == null ? "" : p.Trim().Trim('"'));
            if (p.Length == 0 || busy) return;
            SetBusy(true, "正在读取路径…");
            string pathArg = p;
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/scan?path=" + Uri.EscapeDataString(pathArg), 60000);
                    Node root = ParseTree(json);
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (root == null || root.children.Count == 0) { SetBusy(false, "路径无效: " + pathArg); return; }
                        Node cur = canvas.root;
                        if (cur == null) { canvas.SetTree(root); ShowPage(0); }
                        else { cur.children.Add(root.children[0]); canvas.SetTree(cur); ShowPage(0); }
                        SetBusy(false, "已加入: " + pathArg);
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "加入失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }
        Node ParseTree(string json)
        {
            Dictionary<string, object> d = Json.Parse(json);
            if (!d.ContainsKey("tree")) return null;
            return NodeFrom((Dictionary<string, object>)d["tree"]);
        }
        Node NodeFrom(Dictionary<string, object> d)
        {
            Node n = new Node();
            n.id = S(d, "id"); n.name = S(d, "name"); n.kind = S(d, "kind"); n.path = S(d, "path");
            n.time = S(d, "time"); n.detail = S(d, "detail"); n.del = S(d, "del");
            if (d.ContainsKey("size") && d["size"] != null) n.size = (long)Convert.ToDouble(d["size"]);
            if (d.ContainsKey("refs") && d["refs"] is ArrayList)
                foreach (object r in (ArrayList)d["refs"]) n.refs.Add(Convert.ToString(r));
            if (d.ContainsKey("children") && d["children"] is ArrayList)
                foreach (object c in (ArrayList)d["children"]) n.children.Add(NodeFrom((Dictionary<string, object>)c));
            return n;
        }
        static string S(Dictionary<string, object> d, string k)
        {
            if (!d.ContainsKey(k) || d[k] == null) return "";
            return Convert.ToString(d[k]);
        }

        void ShowHoverNode(Node n, Point screenPt)
        {
            if (n == null) { HideHover(); return; }
            bool isDir = n.kind == "dir";
            string local = AiHelper.LocalDescribe(n.name, n.path, isDir);
            hoverKey = (n.path ?? n.name) + "|" + n.name;
            hoverSeq++;
            int seq = hoverSeq;
            string body = (string.IsNullOrEmpty(n.path) ? "" : n.path + "\r\n") +
                          KindName(n.kind) + " · " + FmtSize(n.size) +
                          (string.IsNullOrEmpty(n.time) ? "" : (" · " + n.time)) +
                          "\r\n作用：" + local;
            canvas.SetHoverCard(n.name, body);
            hoverTip.Text = "悬停右侧文件行也可查看作用";
            AiHelper.DescribeAsync(n.name, n.path, isDir, delegate (string ai)
            {
                if (seq != hoverSeq) return;
                try { canvas.SetHoverCard(n.name, body + "\r\nAI：" + ai); } catch { }
            });
        }
        void ShowHoverRow(ListRow r, Point screenPt)
        {
            if (r == null) { HideHover(); return; }
            string local = AiHelper.LocalDescribe(r.name, r.path, r.isDir);
            hoverKey = (r.path ?? r.name) + "|" + r.name;
            hoverSeq++;
            int seq = hoverSeq;
            string p = r.path ?? "";
            if (p.Length > 90) p = p.Substring(0, 87) + "…";
            string head = r.name + "\r\n" + p + "\r\n" + r.type + " · " + FmtSize(r.size) +
                          (string.IsNullOrEmpty(r.time) ? "" : (" · " + r.time)) + "\r\n作用：" + local;
            hoverTip.Text = head;
            canvas.ClearHoverCard();
            AiHelper.DescribeAsync(r.name, r.path, r.isDir, delegate (string ai)
            {
                if (seq != hoverSeq) return;
                try { hoverTip.Text = head + "\r\nAI：" + ai; } catch { }
            });
        }
        void HideHover()
        {
            hoverSeq++;
            hoverKey = "";
            if (hoverTip != null) hoverTip.Text = "悬停文件查看作用";
            if (canvas != null) canvas.ClearHoverCard();
        }
        void ShowDetail(Node n)
        {
            if (n == null)
            {
                rtbDetail.Text = "在 3D 树中点击节点，或双击下方文件夹浏览\r\n\r\n支持跨盘浏览：点上方的 C: / D: 等盘符即可查看整盘目录。";
                btnQueue.Text = "加入删除队列";
                rows.Clear(); lstFiles.Items.Clear(); lblPath.Text = "未选择"; txtPath.Text = "";
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(n.name);
            sb.AppendLine("层级  L" + n.depth + " · " + KindName(n.kind));
            if (n.path.Length > 0) sb.AppendLine(n.path);
            sb.AppendLine("大小  " + FmtSize(n.size));
            if (n.time.Length > 0) sb.AppendLine("修改  " + n.time);
            if (n.detail.Length > 0) sb.AppendLine("说明  " + n.detail);
            foreach (string r in n.refs) sb.AppendLine("↳ " + r);
            rtbDetail.Text = sb.ToString();
            btnQueue.Text = queue.Contains(n) ? "移出删除队列" : "加入删除队列";
        }
        static string FmtSize(long b)
        {
            if (b <= 0) return "0 B";
            string[] u = { "B", "KB", "MB", "GB", "TB" };
            double v = b; int i = 0;
            while (v >= 1024 && i < 4) { v /= 1024; i++; }
            return (i == 0 ? Math.Round(v).ToString() : v.ToString("0.0")) + " " + u[i];
        }
        string KindName(string k)
        {
            switch (k)
            {
                case "app": return "应用本体 exe";
                case "dir": return "文件夹";
                case "file": return "文件";
                case "config": return "配置文件";
                case "regkey": return "注册表键";
                case "regvalue": return "注册表值";
                case "lnk": return "快捷方式";
                case "proc": return "进程";
                case "prefetch": return "Prefetch";
                case "cache": return "缓存";
                case "item": return "残留项";
                default: return k;
            }
        }

        void ToggleQueue(Node n)
        {
            if (n == null || n.del.Length == 0 || n.del == "none") return;
            if (queue.Contains(n)) queue.Remove(n); else queue.Add(n);
            ShowDetail(n);
            UpdateQueueLabel();
        }
        void UpdateQueueLabel()
        {
            long sz = 0;
            foreach (Node n in queue) sz += n.size;
            lblQueue.Text = queue.Count == 0 ? "队列为空" : ("队列 " + queue.Count + " 项 · " + FmtSize(sz));
            btnDelete.Text = queue.Count > 0 ? ("删除 (" + queue.Count + ")") : "删除";
        }

        void LoadDownloads()
        {
            if (busy) return;
            ShowPage(2);
            SetBusy(true, "读取下载目录…");
            string kw = txtSearch.Text.Trim();
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/downloads?kw=" + Uri.EscapeDataString(kw), 60000);
                    Dictionary<string, object> d = Json.Parse(json);
                    ArrayList arr = (ArrayList)(d.ContainsKey("items") ? d["items"] : new ArrayList());
                    List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();
                    foreach (object o in arr) items.Add((Dictionary<string, object>)o);
                    BeginInvoke((MethodInvoker)delegate
                    {
                        downloads = items;
                        lstDownloads.BeginUpdate(); lstDownloads.Items.Clear();
                        foreach (Dictionary<string, object> o in items)
                            lstDownloads.Items.Add("");
                        lstDownloads.EndUpdate();
                        if (page == 2) pageTitle.Text = "下载目录 · " + items.Count + " 项 · 双击加入树";
                        SetBusy(false, "下载目录: " + items.Count + " 个文件（双击加入树）");
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "读取下载目录失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }
        void LoadOnline(string kw)
        {
            if (kw == null) kw = "";
            ShowPage(3);
            SetBusy(true, "查询在线依赖…");
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/online?kw=" + Uri.EscapeDataString(kw), 20000);
                    Dictionary<string, object> d = Json.Parse(json);
                    ArrayList links = (ArrayList)d["links"];
                    BeginInvoke((MethodInvoker)delegate
                    {
                        lstOnline.BeginUpdate(); lstOnline.Items.Clear(); onlineUrls.Clear();
                        foreach (object o in links)
                        {
                            Dictionary<string, object> l = (Dictionary<string, object>)o;
                            lstOnline.Items.Add(Convert.ToString(l["title"]));
                            onlineUrls.Add(Convert.ToString(l["url"]));
                        }
                        lstOnline.EndUpdate();
                        SetBusy(false, "在线依赖: " + links.Count + " 个链接（双击打开）");
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "在线查询失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }

        void DoVerify()
        {
            if (busy) return;
            SetBusy(true, "核验中…");
            string kw = txtSearch.Text.Trim();
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/verify?kw=" + Uri.EscapeDataString(kw), 60000);
                    Dictionary<string, object> d = Json.Parse(json);
                    BeginInvoke((MethodInvoker)delegate
                    {
                        SetBusy(false, "核验完成");
                        MessageBox.Show(VerifyText(d), "核验结果 · " + kw, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "核验失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }
        string VerifyText(Dictionary<string, object> d)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("相关进程: " + Convert.ToString(d["procCount"]) + " 个 · 占用内存 " + Convert.ToString(d["memMB"]) + " MB");
            if (d.ContainsKey("procs") && d["procs"] is ArrayList)
                foreach (object o in (ArrayList)d["procs"])
                {
                    Dictionary<string, object> p = (Dictionary<string, object>)o;
                    sb.AppendLine("  · " + Convert.ToString(p["name"]) + "  PID " + Convert.ToString(p["pid"]) + "  " + Convert.ToString(p["ws"]) + " MB");
                }
            sb.AppendLine();
            if (d.ContainsKey("diskFree") && d["diskFree"] is Dictionary<string, object>)
                foreach (KeyValuePair<string, object> kv in (Dictionary<string, object>)d["diskFree"])
                    sb.AppendLine("磁盘 " + kv.Key + ": " + kv.Value + " GB 可用");
            if (d.ContainsKey("leftoverCount")) sb.AppendLine("残留路径: " + d["leftoverCount"] + " 个");
            return sb.ToString();
        }
        void DoDelete()
        {
            if (busy || queue.Count == 0) return;
            if (MessageBox.Show("确认彻底删除 " + queue.Count + " 项？\n删除前会自动备份到 NukeTree\\备份3D\\ 目录。", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            SetBusy(true, "正在删除并核验…");
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();
            List<string> paths = new List<string>();
            foreach (Node n in queue)
            {
                Dictionary<string, object> it = new Dictionary<string, object>();
                it["del"] = n.del; it["path"] = n.path; it["name"] = n.name;
                items.Add(it);
                if (n.path.Length > 0) paths.Add(n.path);
            }
            double expectedBytes = 0;
            foreach (Node n in queue) expectedBytes += n.size;
            string body = Json.Stringify(items);
            string kw = txtSearch.Text.Trim();
            string pathsJoined = string.Join("|", paths.ToArray());
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string delJson = Api.Post("http://127.0.0.1:" + Program.Port + "/api/delete", body, 180000);
                    string verJson = Api.Get("http://127.0.0.1:" + Program.Port + "/api/verify?kw=" + Uri.EscapeDataString(kw) + "&paths=" + Uri.EscapeDataString(pathsJoined), 60000);
                    string resJson = Api.Get("http://127.0.0.1:" + Program.Port + "/api/residue?kw=" + Uri.EscapeDataString(kw), 60000);
                    Dictionary<string, object> dr = Json.Parse(delJson);
                    Dictionary<string, object> vr = Json.Parse(verJson);
                    Dictionary<string, object> rr = Json.Parse(resJson);
                    BeginInvoke((MethodInvoker)delegate
                    {
                        SetBusy(false, "删除完成");
                        queue.Clear(); UpdateQueueLabel();
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("删除成功: " + ((ArrayList)dr["deleted"]).Count + " 项");
                        sb.AppendLine("失败: " + ((ArrayList)dr["failed"]).Count + " 项");
                        sb.AppendLine("备份: " + Convert.ToString(dr["backup"]));
                        sb.AppendLine();
                        sb.AppendLine("—— 擦除后核验 ——");
                        sb.AppendLine("残留路径: " + vr["leftoverCount"] + " 个");
                        sb.AppendLine("相关进程: " + vr["procCount"] + " 个 · 占用内存 " + vr["memMB"] + " MB");
                        Dictionary<string, object> before = (Dictionary<string, object>)dr["diskBefore"];
                        Dictionary<string, object> after = (Dictionary<string, object>)dr["diskAfter"];
                        foreach (KeyValuePair<string, object> kv in after)
                        {
                            double b = 0, a = 0;
                            double.TryParse(Convert.ToString(kv.Value), out a);
                            if (before.ContainsKey(kv.Key)) double.TryParse(Convert.ToString(before[kv.Key]), out b);
                            sb.AppendLine("磁盘 " + kv.Key + ": " + b + " GB → " + a + " GB (" + (a - b >= 0 ? "+" : "") + (a - b).ToString("0.00") + " GB)");
                        }
                        sb.AppendLine();
                        sb.AppendLine("—— 残留检测（注册表/服务/任务/启动项/Prefetch/目录） ——");
                        sb.Append(ResidueText(rr, expectedBytes / 1048576.0));
                        int residueCount = 0;
                        try { residueCount = Convert.ToInt32(rr["count"]); } catch { }
                        MessageBox.Show(sb.ToString(), "删除完成 · 已核验", MessageBoxButtons.OK,
                            residueCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                        if (kw.Length > 0) StartScan(kw);
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "删除失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }

        void DoShared()
        {
            if (busy) return;
            string kw = "";
            if (canvas.selected != null && canvas.selected.name.Length > 0) kw = canvas.selected.name;
            else kw = txtSearch.Text.Trim();
            if (kw.Length == 0) { lblStatus.Text = "先在 3D 中选一个应用，或在搜索框输入关键字"; return; }
            SetBusy(true, "分析「" + kw + "」的共用依赖…");
            string q = kw;
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/shared?kw=" + Uri.EscapeDataString(q), 180000);
                    Dictionary<string, object> d = Json.Parse(json);
                    ArrayList items = (ArrayList)(d.ContainsKey("items") ? d["items"] : new ArrayList());
                    List<ListRow> sharedRows = new List<ListRow>();
                    Dictionary<string, Node> byPath = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
                    foreach (Node n in canvas.flat) if (!string.IsNullOrEmpty(n.path)) byPath[n.path] = n;
                    List<Node> hits = new List<Node>();
                    foreach (object o in items)
                    {
                        Dictionary<string, object> it = (Dictionary<string, object>)o;
                        if (!Convert.ToBoolean(it["shared"])) continue;
                        ListRow r = new ListRow();
                        r.name = Convert.ToString(it["name"]);
                        r.path = Convert.ToString(it["path"]);
                        try { r.size = (long)Convert.ToDouble(it["size"]); } catch { }
                        StringBuilder rb = new StringBuilder("共用：");
                        if (it.ContainsKey("refs") && it["refs"] is ArrayList)
                        {
                            int c = 0;
                            foreach (object ro in (ArrayList)it["refs"]) { if (c > 0) rb.Append(", "); rb.Append(Convert.ToString(ro)); c++; if (c >= 3) { rb.Append("…"); break; } }
                        }
                        r.type = rb.ToString();
                        sharedRows.Add(r);
                        Node nn;
                        if (byPath.TryGetValue(r.path, out nn)) hits.Add(nn);
                    }
                    int total = Convert.ToInt32(d["total"]);
                    int sharedCount = Convert.ToInt32(d["sharedCount"]);
                    BeginInvoke((MethodInvoker)delegate
                    {
                        ShowPage(0);
                        rows.Clear(); rows.AddRange(sharedRows);
                        lstFiles.BeginUpdate(); lstFiles.Items.Clear();
                        foreach (ListRow r in rows) lstFiles.Items.Add(r.name);
                        lstFiles.EndUpdate();
                        lblPath.Text = "共用依赖 · " + sharedCount + " / " + total + " 项（其它应用也在用）";
                        hoverTip.Text = "被共用的依赖不要删，否则会影响其它应用";
                        canvas.SetMatches(hits);
                        SetBusy(false, "共用依赖: " + sharedCount + " / " + total + " 项被其它应用或系统共用");
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "共用依赖分析失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }
        string ResidueText(Dictionary<string, object> rr, double expectedMB)
        {
            StringBuilder sb = new StringBuilder();
            int cnt = 0;
            try { cnt = Convert.ToInt32(rr["count"]); } catch { }
            sb.AppendLine("回收空间: " + (expectedMB >= 0 ? ("选中项约 " + expectedMB.ToString("0.0") + " MB") : ""));
            if (cnt == 0)
            {
                sb.AppendLine("✓ 未发现残留（注册表卸载项 / 服务 / 计划任务 / 启动项 / Prefetch / 残留目录 = 0）");
            }
            else
            {
                sb.AppendLine("⚠ 仍有 " + cnt + " 项残留，下面逐项列出：");
                int shown = 0;
                if (rr["items"] is ArrayList)
                    foreach (object o in (ArrayList)rr["items"])
                    {
                        if (shown++ >= 14) { sb.AppendLine("  …（其余 " + (cnt - 14) + " 项省略）"); break; }
                        Dictionary<string, object> it = (Dictionary<string, object>)o;
                        sb.AppendLine("  [" + Convert.ToString(it["type"]) + "] " + Convert.ToString(it["name"]));
                        string pp = Convert.ToString(it["path"]);
                        if (pp.Length > 70) pp = pp.Substring(0, 67) + "…";
                        sb.AppendLine("      " + pp);
                    }
            }
            return sb.ToString();
        }
        void RefreshPlugins()
        {
            try
            {
                pluginItems = Plugins.Load();
                lstPlugins.BeginUpdate();
                lstPlugins.Items.Clear();
                foreach (PluginItem p in pluginItems) lstPlugins.Items.Add(p.Name);
                lstPlugins.EndUpdate();
                lblPluginInfo.Text = "已加载 " + pluginItems.Count + " 个插件 · 放到 plugins\\<名字>\\ 下（plugin.json + 脚本），点刷新即可";
                pageTitle.Text = "插件 · " + pluginItems.Count + " 个";
            }
            catch (Exception ex) { lblPluginInfo.Text = "加载插件失败: " + ex.Message; }
        }
        void DrawPluginRow(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= pluginItems.Count) return;
            PluginItem p = pluginItems[e.Index];
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Rectangle b = e.Bounds;
            using (SolidBrush bg = new SolidBrush(sel ? Theme.Panel2 : Theme.Bg)) e.Graphics.FillRectangle(bg, b);
            if (sel) using (SolidBrush ab = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(ab, b.X + S(6), b.Y + S(10), S(3), b.Height - S(20));
            using (Font nf = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, p.Name, nf, new Rectangle(b.X + S(18), b.Y + S(7), b.Width - S(160), S(22)),
                    sel ? Theme.Txt : Color.FromArgb(230, 234, 240), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using (Font vf = new Font("Segoe UI", 8.2f))
                TextRenderer.DrawText(e.Graphics, "v" + p.Version + (string.IsNullOrEmpty(p.Author) ? "" : ("  ·  " + p.Author)) + "  ·  " + p.Id,
                    vf, new Rectangle(b.X + S(18), b.Y + S(28), b.Width - S(160), S(16)), Theme.Dim,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using (Font df = new Font("Segoe UI", 8.5f))
                TextRenderer.DrawText(e.Graphics, p.Description, df, new Rectangle(b.X + S(18), b.Y + S(43), b.Width - S(160), S(15)),
                    Color.FromArgb(168, 176, 186), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            string st = p.Enabled ? "已启用" : "已停用";
            Color sc = p.Enabled ? Color.FromArgb(150, 190, 150) : Color.FromArgb(160, 160, 168);
            using (Font sf2 = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, st, sf2, new Rectangle(b.Right - S(110), b.Y, S(92), b.Height), sc,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            using (SolidBrush ln = new SolidBrush(Color.FromArgb(18, 48, 52, 58))) e.Graphics.FillRectangle(ln, b.X + S(10), b.Bottom - 1, b.Width - S(20), 1);
        }
        void RunSelectedPlugin()
        {
            int i = lstPlugins.SelectedIndex;
            if (i < 0 || i >= pluginItems.Count) { lblStatus.Text = "先在列表里选一个插件"; return; }
            PluginItem p = pluginItems[i];
            if (!p.Enabled) { lblStatus.Text = "插件已停用，先启用再运行"; return; }
            Node nd = canvas.selected;
            string path = nd == null ? "" : nd.path;
            if (string.IsNullOrEmpty(path) && (txtPath.Text ?? "").Trim().Length > 0) path = txtPath.Text.Trim();
            if (string.IsNullOrEmpty(path)) { lblStatus.Text = "先在 3D 或文件面板里选择一个目标"; return; }
            string name = nd == null ? System.IO.Path.GetFileName(path) : nd.name;
            lblStatus.Text = "运行插件「" + p.Name + "」…";
            Thread t = new Thread(delegate ()
            {
                string output = Plugins.Run(p, path, name);
                BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "插件「" + p.Name + "」执行完成";
                    MessageBox.Show(output + "\r\n\r\n目标: " + path, "插件 · " + p.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            });
            t.IsBackground = true; t.Start();
        }
        void TogglePlugin()
        {
            int i = lstPlugins.SelectedIndex;
            if (i < 0 || i >= pluginItems.Count) { lblStatus.Text = "先在列表里选一个插件"; return; }
            pluginItems[i].Enabled = !pluginItems[i].Enabled;
            Plugins.SaveState(pluginItems);
            RefreshPlugins();
            lstPlugins.SelectedIndex = i;
            lblStatus.Text = "插件「" + pluginItems[i].Name + "」已" + (pluginItems[i].Enabled ? "启用" : "停用");
        }
        void OpenPluginDir()
        {
            try
            {
                Plugins.EnsureSamples();
                System.Diagnostics.Process.Start("explorer.exe", "\"" + Plugins.Dir + "\"");
            }
            catch (Exception ex) { lblStatus.Text = "打开插件目录失败: " + ex.Message; }
        }
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        void ApplyDarkTitle()
        {
            try
            {
                int on = 1;
                DwmSetWindowAttribute(Handle, 20, ref on, 4);
                int cap = ColorTranslator.ToWin32(Theme.Bg);
                DwmSetWindowAttribute(Handle, 35, ref cap, 4);
                int tx = ColorTranslator.ToWin32(Theme.Txt);
                DwmSetWindowAttribute(Handle, 36, ref tx, 4);
            }
            catch { }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDarkTitle();
        }
        void ShowSettings()
        {
            using (SettingsForm f = new SettingsForm(cfg))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    cfg = SettingsStore.Load();
                    Theme.Apply(cfg);
                    ApplyThemeLive();
                    canvas.Invalidate();
                    lblStatus.Text = "设置已保存并生效";
                }
            }
        }
        void ApplyThemeLive()
        {
            BackColor = Theme.Bg;
            side.BackColor = Theme.Side;
            header.BackColor = Theme.Bg;
            bottom.BackColor = Theme.Bg;
            rightPan.BackColor = Theme.Bg;
            if (bottomInfo != null) bottomInfo.BackColor = Theme.Bg;
            lstApps.BackColor = Theme.Bg; lstDownloads.BackColor = Theme.Bg; lstOnline.BackColor = Theme.Bg; lstFiles.BackColor = Theme.Bg; lstPlugins.BackColor = Theme.Bg;
            rtbDetail.BackColor = Theme.Panel;
            side.Invalidate(); header.Invalidate(); bottom.Invalidate(); rightPan.Invalidate(); colHeader.Invalidate();
            ApplyDarkTitle();
            Invalidate(true);
        }
        void LoadAllDrives()
        {
            if (busy) return;
            ShowPage(0);
            SetBusy(true, "正在构建全盘总览…");
            Thread t = new Thread(delegate ()
            {
                try
                {
                    Node root = new Node();
                    root.id = "all"; root.name = "本机全部文件"; root.kind = "root";
                    foreach (DriveInfo di in DriveInfo.GetDrives())
                    {
                        try
                        {
                            if (!di.IsReady) continue;
                            string lp = di.RootDirectory.FullName;
                            Node dn = new Node();
                            dn.id = "d_" + lp; dn.name = lp.TrimEnd('\\'); dn.kind = "dir"; dn.path = lp; dn.del = "dir";
                            try
                            {
                                string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/scan?path=" + Uri.EscapeDataString(lp), 60000);
                                Node r2 = ParseTree(json);
                                if (r2 != null && r2.children.Count > 0) dn.children = r2.children[0].children;
                            }
                            catch { }
                            root.children.Add(dn);
                        }
                        catch { }
                    }
                    BeginInvoke((MethodInvoker)delegate
                    {
                        canvas.SetTree(root);
                        queue.Clear(); UpdateQueueLabel(); ShowDetail(null);
                        if (root.children.Count > 0) OnNodeSelected(root.children[0]);
                        SetBusy(false, "全盘总览 · " + root.children.Count + " 个磁盘（双击文件夹逐层展开）");
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "全盘总览失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }
        void ExpandNode(Node n)
        {
            if (n == null || busy) return;
            if (n.children.Count > 0) return;
            if (string.IsNullOrEmpty(n.path) || !Directory.Exists(n.path)) return;
            string p = n.path;
            SetBusy(true, "展开 " + p + " …");
            Thread t = new Thread(delegate ()
            {
                try
                {
                    string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/scan?path=" + Uri.EscapeDataString(p), 60000);
                    Node r = ParseTree(json);
                    List<Node> kids = (r != null && r.children.Count > 0) ? r.children[0].children : new List<Node>();
                    BeginInvoke((MethodInvoker)delegate
                    {
                        n.children = kids;
                        canvas.SetTree(canvas.root);
                        canvas.selected = n;
                        canvas.Invalidate();
                        OnNodeSelected(n);
                        SetBusy(false, "展开: " + kids.Count + " 项（继续双击下钻）");
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "展开失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }
        void DoFind()
        {
            string q = (txtFind.Text ?? "").Trim();
            if (q.Length == 0) { canvas.ClearMatches(); lblStatus.Text = "已清除 3D 搜索"; return; }
            List<Node> hits = new List<Node>();
            foreach (Node n in canvas.flat)
            {
                if ((n.name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (n.path ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) hits.Add(n);
            }
            canvas.SetMatches(hits);
            if (hits.Count > 0)
            {
                canvas.selected = hits[0];
                canvas.Invalidate();
                OnNodeSelected(hits[0]);
                ShowPage(0);
                lblStatus.Text = "找到 " + hits.Count + " 个匹配（已高亮）";
            }
            else lblStatus.Text = "未找到「" + q + "」";
        }
        void AddDroppedPaths(string[] paths)
        {
            if (paths == null || paths.Length == 0 || busy) return;
            SetBusy(true, "正在读取拖入的 " + paths.Length + " 个路径…");
            string[] ps = paths;
            Thread t = new Thread(delegate ()
            {
                try
                {
                    List<Node> added = new List<Node>();
                    foreach (string p in ps)
                    {
                        try
                        {
                            string json = Api.Get("http://127.0.0.1:" + Program.Port + "/api/scan?path=" + Uri.EscapeDataString(p), 60000);
                            Node r = ParseTree(json);
                            if (r != null && r.children.Count > 0) added.Add(r.children[0]);
                        }
                        catch { }
                    }
                    BeginInvoke((MethodInvoker)delegate
                    {
                        Node target = canvas.root;
                        if (target == null)
                        {
                            target = new Node();
                            target.id = "drop"; target.name = "拖入的文件"; target.kind = "root"; target.del = "none";
                        }
                        foreach (Node a in added) target.children.Add(a);
                        canvas.SetTree(target);
                        ShowPage(0);
                        if (added.Count > 0) OnNodeSelected(added[added.Count - 1]);
                        SetBusy(false, "已拖入 " + added.Count + " 项到 3D");
                    });
                }
                catch (Exception ex) { BeginInvoke((MethodInvoker)delegate { SetBusy(false, "拖入失败: " + ex.Message); }); }
            });
            t.IsBackground = true; t.Start();
        }
        void SetBusy(bool b, string status)
        {
            busy = b;
            lblStatus.Text = status;
            btnScan.Enabled = !b; btnAddPath.Enabled = !b; btnVerify.Enabled = !b; btnDelete.Enabled = !b; btnClear.Enabled = !b;
            btnGo.Enabled = !b; btnUp.Enabled = !b; btnRefresh.Enabled = !b;
            UseWaitCursor = b;
        }
    }
}
