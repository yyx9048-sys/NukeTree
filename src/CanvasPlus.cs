using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AppNuke3D
{
    class TreeCanvas2 : Panel
    {
        public Node root;
        public List<Node> flat = new List<Node>();
        public double yaw = 0.85, pitch = 0.55, zoom = 1.0;
        public bool autoRotate = false;
        public bool busy = false;
        public Node selected, hover;
        public Font fSmall, fNormal, fBig, fTiny;
        public event EventHandler SelectionChanged;
        public event Action<Node> NodeDoubleClicked;
        public HashSet<Node> matches = new HashSet<Node>();
        public string HoverCardTitle = "", HoverCardBody = "";
        Font fCardTitle, fCardBody;
        public event Action<Node, Point> HoverChanged;
        public event Action HoverEnded;

        static readonly double[] RAD = { 0, 150, 305, 470, 600 };
        static readonly double[] BY = { 185, 90, -110, -400, -650 };

        double targetYaw = 0.85, targetPitch = 0.55, targetZoom = 1.0;
        bool dragging = false, moved = false;
        int lastX, lastY, hoverIdx = -1;
        System.Windows.Forms.Timer timer;
        DateTime start = DateTime.Now;
        float dpi = 1f;

        public TreeCanvas2()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(13, 16, 21);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            fSmall = new Font("Segoe UI", 8f);
            fNormal = new Font("Segoe UI", 9f);
            fBig = new Font("Segoe UI", 11f, FontStyle.Bold);
            fTiny = new Font("Segoe UI", 7.5f);
            fCardTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            fCardBody = new Font("Segoe UI", 8.5f);
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 33;
            timer.Tick += delegate
            {
                bool spinning = autoRotate && !dragging;
                if (spinning) targetYaw += 0.0018;
                double dy = targetYaw - yaw, dp = targetPitch - pitch, dz = targetZoom - zoom;
                yaw += dy * 0.18;
                pitch += dp * 0.18;
                zoom += dz * 0.18;
                bool moving = Math.Abs(dy) > 0.0006 || Math.Abs(dp) > 0.0006 || Math.Abs(dz) > 0.0012;
                if (spinning || dragging || moving) Invalidate();
            };
            timer.Start();
        }

        public void SetTree(Node r)
        {
            root = r; flat.Clear();
            if (r != null)
            {
                Flatten(r, 0, 0, Math.PI * 2, null);
                foreach (Node n in flat)
                {
                    double rr = RAD[Math.Min(n.depth, 4)];
                    int ci = (n.parent == null) ? 0 : n.parent.children.IndexOf(n);
                    int sib = (n.parent == null) ? 1 : Math.Max(1, n.parent.children.Count);
                    if (n.depth >= 1) rr *= (1 + Math.Min(0.45, sib * 0.007));
                    if (n.depth >= 2) rr += ((ci % 3) - 1) * 12;
                    double seed = ((n.name.Length * 37 + ci * 53) % 100) / 100.0 - 0.5;
                    n.angle += seed * 0.05;
                    n.yOff = ((ci % 3) - 1) * 11;
                    n.x = rr * Math.Cos(n.angle);
                    double wav = n.depth == 2 ? 42 : (n.depth == 3 ? 60 : 0);
                    n.y = BY[Math.Min(n.depth, 4)] + n.yOff + Math.Sin(n.angle * 2.2) * wav;
                    n.z = rr * Math.Sin(n.angle);
                }
            }
            if (Width > 100 && Height > 100 && flat.Count > 1)
                targetZoom = Math.Max(0.55, Math.Min(2.4, Math.Min(Width, Height) / 720.0));
            Invalidate();
        }

        void Flatten(Node n, int depth, double a0, double a1, Node parent)
        {
            n.depth = depth; n.angle = (a0 + a1) / 2; n.parent = parent;
            flat.Add(n);
            if (n.children.Count == 0) return;
            double span = (a1 - a0) / n.children.Count;
            for (int i = 0; i < n.children.Count; i++)
                Flatten(n.children[i], depth + 1, a0 + i * span, a0 + (i + 1) * span, n);
        }

        void Project(Node n, out float px, out float py, out float ps, out double pz)
        {
            int W = Width, H = Height;
            double cy = Math.Cos(yaw), sy = Math.Sin(yaw), cp = Math.Cos(pitch), sp = Math.Sin(pitch);
            double x1 = n.x * cy + n.z * sy;
            double z1 = -n.x * sy + n.z * cy;
            double y1 = n.y * cp - z1 * sp;
            double z2 = n.y * sp + z1 * cp;
            double d = 1050, f = 780 * zoom;
            ps = (float)(f / Math.Max(120, z2 + d));
            px = (float)(W / 2 + x1 * ps);
            py = (float)(H / 2 - y1 * ps + 60);
            pz = z2;
        }

        static Color KindColor(string k)
        {
            switch (k)
            {
                case "app": return Color.FromArgb(232, 197, 130);
                case "dir": return Color.FromArgb(126, 174, 230);
                case "config": return Color.FromArgb(118, 176, 178);
                case "regkey": return Color.FromArgb(168, 148, 208);
                case "regvalue": return Color.FromArgb(190, 172, 216);
                case "lnk": return Color.FromArgb(116, 190, 230);
                case "proc": return Color.FromArgb(214, 138, 138);
                case "prefetch": return Color.FromArgb(139, 147, 161);
                case "cache": return Color.FromArgb(150, 166, 206);
                default: return Color.FromArgb(154, 164, 178);
            }
        }

        static Color Light(Color c, float k)
        {
            return Color.FromArgb(c.A, Math.Min(255, (int)(c.R + (255 - c.R) * k)), Math.Min(255, (int)(c.G + (255 - c.G) * k)), Math.Min(255, (int)(c.B + (255 - c.B) * k)));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try { PaintTree(e); } catch (Exception ex) { Program.Log("PAINT EX: " + ex.Message); }
        }

        void PaintBackground(Graphics g)
        {
            Rectangle R = ClientRectangle;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(2, R.Width), Math.Max(2, R.Height)),
                Theme.BgTop, Theme.BgBottom, 90f))
                g.FillRectangle(b, R);

            // 中心柔和光晕（替换原来的四角灰暗）
            using (GraphicsPath p = new GraphicsPath())
            {
                float cx = R.Width / 2f, cy = R.Height / 2f;
                float rad = Math.Min(R.Width, R.Height) * 0.65f;
                p.AddEllipse(cx - rad, cy - rad, rad * 2, rad * 2);
                using (PathGradientBrush pb = new PathGradientBrush(p))
                {
                    pb.CenterColor = Color.FromArgb(26, 255, 255, 255);
                    pb.SurroundColors = new Color[] { Color.FromArgb(0, 255, 255, 255) };
                    g.FillPath(pb, p);
                }
            }

            // 同心参考环（放射状视觉）
            using (Pen ring = new Pen(Color.FromArgb(16, 255, 255, 255), 1f))
            {
                for (int i = 1; i <= 5; i++)
                {
                    float rad = Math.Min(Width, Height) * (0.10f + i * 0.09f);
                    g.DrawEllipse(ring, Width / 2f - rad, Height / 2f - rad, rad * 2, rad * 2);
                }
            }

            // 稀疏星点
            Random rnd = new Random(12345);
            using (SolidBrush dot = new SolidBrush(Color.FromArgb(26, 255, 255, 255)))
            {
                for (int i = 0; i < 140; i++)
                {
                    float x = (float)rnd.NextDouble() * R.Width;
                    float y = (float)rnd.NextDouble() * R.Height;
                    float r = (float)rnd.NextDouble() * 1.3f + 0.4f;
                    g.FillEllipse(dot, x, y, r, r);
                }
            }
        }
        void PaintTree(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            dpi = g.DpiX / 96f;
            PaintBackground(g);

            if (root == null || flat.Count == 0)
            {
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (Font f1 = new Font("Segoe UI", 11.5f))
                    using (Font f2 = new Font("Segoe UI", 9f))
                    {
                        g.DrawString("输入应用名并点「扫描」，或把文件/文件夹拖进这里", f1, new SolidBrush(Color.FromArgb(200, 210, 218, 230)),
                            new RectangleF(0, Height / 2 - 60, Width, 30), sf);
                        g.DrawString("或从左侧应用列表 / 手动路径加入\n\nL1 = 应用本体 exe      L2 = 关联到它的全部内容      L3 = 上一层内容的细节",
                            f2, new SolidBrush(Color.FromArgb(140, 139, 147, 163)),
                            new RectangleF(0, Height / 2 - 20, Width, 120), sf);
                    }
                }
                return;
            }

            float[] px = new float[flat.Count], py = new float[flat.Count], ps = new float[flat.Count];
            double[] pz = new double[flat.Count];
            for (int i = 0; i < flat.Count; i++)
            {
                float a, b, c; double d;
                Project(flat[i], out a, out b, out c, out d);
                px[i] = a; py[i] = b; ps[i] = c; pz[i] = d;
                flat[i].sx = a; flat[i].sy = b; flat[i].sz = d;
                float baseR = flat[i].depth == 1 ? 15f : flat[i].depth == 2 ? 9.5f : flat[i].depth == 3 ? 7f : 4.5f;
                flat[i].sr = Math.Max(2f, baseR * dpi * (c / 0.74f));
            }

            double t = (DateTime.Now - start).TotalSeconds;

            // 焦点模式：选中节点时其它分支淡化
            HashSet<Node> focus = new HashSet<Node>();
            if (selected != null)
            {
                Node pp = selected;
                while (pp != null) { focus.Add(pp); pp = pp.parent; }
                foreach (Node c in selected.children) focus.Add(c);
            }
            bool hasFocus = selected != null;

            // 连线：贝塞尔曲线，按深度渐隐
            for (int i = 0; i < flat.Count; i++)
            {
                Node n = flat[i];
                if (n.parent == null) continue;
                int pi = flat.IndexOf(n.parent);
                if (pi < 0) continue;
                bool sel = (selected != null && (n == selected || n.parent == selected));
                int alpha = sel ? 235 : (n.depth <= 2 ? 110 : 62);
                if (hasFocus && !focus.Contains(n)) alpha = 7;
                Color ec = sel ? Theme.Accent : Color.FromArgb(120, 145, 178);
                using (Pen pen = new Pen(Color.FromArgb(alpha, ec.R, ec.G, ec.B), (sel ? 1.8f : (n.depth <= 2 ? 1.0f : 0.7f)) * dpi))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                    int seed = (i * 37 + n.name.Length * 13) & 0xFFFF;
                    float jx = ((seed % 7) - 3) * 1.1f * (float)dpi;
                    float jy = (((seed >> 3) % 7) - 3) * 1.1f * (float)dpi;
                    g.DrawLine(pen, px[pi] + jx * 0.4f, py[pi] + jy * 0.4f, px[i] - jx, py[i] - jy);
                }
            }

            // 节点排序：远到近
            int[] order = new int[flat.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, delegate (int a, int b) { return pz[b].CompareTo(pz[a]); });

            List<RectangleF> labels = new List<RectangleF>();
            for (int oi = 0; oi < order.Length; oi++)
            {
                int i = order[oi];
                Node n = flat[i];
                bool isSel = (n == selected), isHov = (n == hover);
                float r = flat[i].sr;
                Color col = KindColor(n.kind);
                bool inFocus = !hasFocus || focus.Contains(n);
                float fade = inFocus ? (n.depth >= 3 ? 0.72f : 1f) : 0.20f;
                bool hasSearch = matches.Count > 0;
                if (hasSearch) fade = matches.Contains(n) ? 1f : 0.16f;
                bool pulse = (n.depth == 1 && !isSel);

                if (isSel || isHov)
                {
                    // 外发光
                    for (int k = 5; k >= 1; k--)
                    {
                        float rr = r + k * 3.2f * dpi;
                        int a = isSel ? (int)(10 + 6 * (6 - k)) : (int)(6 + 4 * (6 - k));
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(a, col))) g.FillEllipse(b, px[i] - rr, py[i] - rr, rr * 2, rr * 2);
                    }
                }

                // 球体：径向渐变 + 高光 + 边缘光
                RectangleF rc = new RectangleF(px[i] - r, py[i] - r, r * 2, r * 2);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(rc);
                    using (PathGradientBrush pb = new PathGradientBrush(gp))
                    {
                        pb.CenterPoint = new PointF(px[i] - r * 0.32f, py[i] - r * 0.36f);
                        pb.CenterColor = Light(col, 0.55f);
                        pb.SurroundColors = new Color[] { Color.FromArgb((int)(255 * fade), col.R / 2, col.G / 2, col.B / 2) };
                        g.FillPath(pb, gp);
                    }
                    using (Pen rim = new Pen(Color.FromArgb((int)(180 * fade), Light(col, 0.35f)), 1.1f * dpi))
                        g.DrawPath(rim, gp);
                }
                if (pulse)
                {
                    float pr = r + (float)(2.5 + 1.6 * Math.Sin(t * 2.2)) * dpi;
                    using (Pen pp = new Pen(Color.FromArgb(70, col), 1.4f * dpi)) g.DrawEllipse(pp, px[i] - pr, py[i] - pr, pr * 2, pr * 2);
                }
                if (hasSearch && matches.Contains(n))
                {
                    using (Pen mp = new Pen(Theme.Accent, 2f * dpi)) g.DrawEllipse(mp, px[i] - r - 3 * dpi, py[i] - r - 3 * dpi, (r + 3 * dpi) * 2, (r + 3 * dpi) * 2);
                }
                if (isSel)
                {
                    using (Pen sp = new Pen(Theme.Accent, 2.2f * dpi)) g.DrawEllipse(sp, px[i] - r - 4 * dpi, py[i] - r - 4 * dpi, (r + 4 * dpi) * 2, (r + 4 * dpi) * 2);
                }

                bool want = isSel || isHov || n.depth == 1 || (hasFocus && n.parent == selected) || (zoom >= 1.9 && n.depth <= 2) || (hasSearch && matches.Contains(n));
                if (!inFocus) want = false;
                if (want)
                {
                    string txt = n.name.Length > 22 ? n.name.Substring(0, 21) + "…" : n.name;
                    Font f = n.depth == 1 ? fBig : (n.depth == 2 ? fNormal : fSmall);
                    SizeF sz = g.MeasureString(txt, f);
                    float lx = px[i] + r + 7 * dpi, ly = py[i] - sz.Height / 2;
                    RectangleF pill = new RectangleF(lx, ly, sz.Width + 14 * dpi, sz.Height + 4 * dpi);
                    bool clash = false;
                    foreach (RectangleF rr in labels) if (rr.IntersectsWith(pill)) { clash = true; break; }
                    if (isSel || isHov || n.depth == 1 || !clash)
                    {
                        labels.Add(pill);
                        // 圆角药丸底
                        using (GraphicsPath pp = Round(pill, (int)(pill.Height / 2)))
                        {
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(190, 8, 10, 14))) g.FillPath(b, pp);
                            using (Pen bp = new Pen(isSel ? Theme.Accent : Color.FromArgb(90, 255, 255, 255), 1f * dpi)) g.DrawPath(bp, pp);
                        }
                        using (SolidBrush dotb = new SolidBrush(col)) g.FillEllipse(dotb, lx + 5 * dpi, ly + pill.Height / 2 - 2.5f * dpi, 5 * dpi, 5 * dpi);
                        Color tc = isSel ? Theme.Accent : (n.depth == 1 ? Color.FromArgb(255, 220, 205) : (n.depth == 2 ? Color.FromArgb(210, 220, 232) : Color.FromArgb(155, 165, 180)));
                        g.DrawString(txt, f, new SolidBrush(tc), lx + 13 * dpi, ly + 2 * dpi);
                    }
                }
            }

            // 图例
            string[] kinds = { "app", "dir", "config", "regkey", "lnk", "proc", "file" };
            string[] names2 = { "应用本体 exe", "目录", "配置", "注册表", "快捷方式", "进程", "文件" };
            float lx2 = 16 * dpi, ly2 = Height - 36 * dpi;
            using (Font lf = new Font("Segoe UI", 8.5f))
            {
                for (int i = 0; i < kinds.Length; i++)
                {
                    float w = g.MeasureString(names2[i], lf).Width + 22 * dpi;
                    RectangleF pr = new RectangleF(lx2, ly2, w, 22 * dpi);
                    using (GraphicsPath gp = Round(pr, (int)(11 * dpi)))
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(120, 12, 15, 20))) g.FillPath(b, gp);
                        using (Pen p = new Pen(Color.FromArgb(60, 255, 255, 255), 0.8f * dpi)) g.DrawPath(p, gp);
                    }
                    using (SolidBrush b = new SolidBrush(KindColor(kinds[i]))) g.FillEllipse(b, lx2 + 8 * dpi, ly2 + 8 * dpi, 6 * dpi, 6 * dpi);
                    g.DrawString(names2[i], lf, new SolidBrush(Color.FromArgb(190, 200, 208, 220)), lx2 + 18 * dpi, ly2 + 3 * dpi);
                    lx2 += w + 8 * dpi;
                }
            }
            using (Font hf = new Font("Segoe UI", 8f))
                g.DrawString("拖动旋转 · 滚轮缩放 · 点击选中", hf, new SolidBrush(Color.FromArgb(110, 139, 147, 163)), 18 * dpi, 44 * dpi);

            if (HoverCardTitle.Length > 0)
            {
                float cw = 430 * dpi, ch = 108 * dpi;
                float cx = 16 * dpi, cy = Height - ch - 46 * dpi;
                using (GraphicsPath gp = Round(new RectangleF(cx, cy, cw, ch), (int)(9 * dpi)))
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(225, 14, 16, 20))) g.FillPath(b, gp);
                    using (Pen p = new Pen(Color.FromArgb(110, Theme.Accent), 1f * dpi)) g.DrawPath(p, gp);
                }
                using (SolidBrush tb = new SolidBrush(Theme.Txt)) g.DrawString(HoverCardTitle, fCardTitle, tb, cx + 12 * dpi, cy + 8 * dpi);
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(205, 214, 222, 232)))
                {
                    RectangleF br = new RectangleF(cx + 12 * dpi, cy + 30 * dpi, cw - 24 * dpi, ch - 36 * dpi);
                    g.DrawString(HoverCardBody, fCardBody, bb, br);
                }
            }
        }

        public void SetHoverCard(string title, string body) { HoverCardTitle = title ?? ""; HoverCardBody = body ?? ""; Invalidate(); }
        public void ClearHoverCard() { if (HoverCardTitle.Length > 0 || HoverCardBody.Length > 0) { HoverCardTitle = ""; HoverCardBody = ""; Invalidate(); } }
        public void SetMatches(List<Node> hit)
        {
            matches = new HashSet<Node>();
            if (hit != null) foreach (Node n in hit) matches.Add(n);
            Invalidate();
        }
        public void ClearMatches() { matches.Clear(); Invalidate(); }
        public void ZoomBy(double f) { targetZoom = Math.Max(0.35, Math.Min(3.2, targetZoom * f)); }
        public void ResetView() { targetYaw = 0.85; targetPitch = 0.55; targetZoom = 1.0; }
        public int ZoomPercent { get { return (int)Math.Round(targetZoom * 100); } }

        static GraphicsPath Round(RectangleF r, int rad)
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

        Node Pick(int mx, int my)
        {
            Node best = null; double bd = double.MaxValue;
            foreach (Node n in flat)
            {
                double dx = n.sx - mx, dy = n.sy - my;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d < n.sr + 12 * dpi && d < bd) { bd = d; best = n; }
            }
            return best;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            dragging = true; moved = false; lastX = e.X; lastY = e.Y;
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                int dx = e.X - lastX, dy = e.Y - lastY;
                if (Math.Abs(dx) + Math.Abs(dy) > 2) moved = true;
                targetYaw += dx * 0.007;
                targetPitch = Math.Max(-1.25, Math.Min(1.25, targetPitch + dy * 0.007));
                lastX = e.X; lastY = e.Y;
            }
            else
            {
                Node n = Pick(e.X, e.Y);
                if (n != hover)
                {
                    hover = n;
                    Invalidate();
                    if (n != null) { if (HoverChanged != null) HoverChanged(n, PointToScreen(e.Location)); }
                    else { if (HoverEnded != null) HoverEnded(); }
                }
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            if (!moved)
            {
                Node n = Pick(e.X, e.Y);
                selected = n;
                Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
            base.OnMouseUp(e);
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            Node n = Pick(e.X, e.Y);
            if (n != null && NodeDoubleClicked != null) NodeDoubleClicked(n);
            base.OnMouseDoubleClick(e);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            targetZoom = Math.Max(0.35, Math.Min(3.2, targetZoom * (float)Math.Exp(-e.Delta * 0.0012)));
            base.OnMouseWheel(e);
        }
        protected override void OnResize(EventArgs eventargs)
        {
            Invalidate();
            base.OnResize(eventargs);
        }
    }
}
