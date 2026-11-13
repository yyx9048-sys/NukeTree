using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace AppNuke3D
{
    class Node
    {
        public string id = "", name = "", kind = "", path = "", time = "", detail = "", del = "";
        public long size = 0;
        public List<string> refs = new List<string>();
        public List<Node> children = new List<Node>();
        public Node parent;
        public int depth = 0;
        public double angle = 0;
        public double x, y, z, yOff;
        public float sx, sy, sr;
        public double sz;
    }

    static class Json
    {
        static JavaScriptSerializer ser = new JavaScriptSerializer();
        static Json() { ser.MaxJsonLength = int.MaxValue; }
        public static Dictionary<string, object> Parse(string s) { return ser.Deserialize<Dictionary<string, object>>(s); }
        public static string Stringify(object o) { return ser.Serialize(o); }
    }

    static class Api
    {
        public static string Get(string url, int timeoutMs)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Proxy = null; req.Timeout = timeoutMs; req.ReadWriteTimeout = timeoutMs;
            using (HttpWebResponse r = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(r.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }
        public static string Post(string url, string body, int timeoutMs)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Proxy = null; req.Method = "POST"; req.ContentType = "application/json";
            req.Timeout = timeoutMs; req.ReadWriteTimeout = timeoutMs;
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            req.ContentLength = bytes.Length;
            using (Stream s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
            using (HttpWebResponse r = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(r.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }
    }

    static class Program
    {
        public const int Port = 18765;
        static System.Diagnostics.Process server;
        public static string Dir;

        [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch { try { SetProcessDPIAware(); } catch { } }
            Dir = AppDomain.CurrentDomain.BaseDirectory;
            Log("=== start bit=" + (IntPtr.Size * 8));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs te) { Log("THREAD EX: " + te.Exception.ToString()); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs ue) { Log("UNHANDLED: " + ue.ExceptionObject.ToString()); };
            string kw = "";
            foreach (string a in args)
            {
                if (a.StartsWith("--kw=")) kw = a.Substring(5);
                if (a == "--settings") kw = "__settings";
                if (a == "--alldisk") kw = "__alldisk";
                if (a == "--apps") kw = "__apps";
                if (a == "--downloads") kw = "__downloads";
                if (a == "--plugins") kw = "__plugins";
            }

            if (!Ping())
            {
                string script = Path.Combine(Dir, "NukeTree.ps1");
                if (!File.Exists(script)) { MessageBox.Show("找不到后端脚本:\n" + script); return; }
                try
                {
                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                    psi.FileName = "powershell.exe";
                    psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + script + "\" -NoBrowser";
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.WorkingDirectory = Dir;
                    server = System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex) { MessageBox.Show("后端启动失败:\n" + ex.Message); return; }
                for (int i = 0; i < 100 && !Ping(); i++) Thread.Sleep(300);
                if (!Ping()) { MessageBox.Show("后端服务未就绪(端口 " + Port + " 可能被占用)"); StopServer(); return; }
            }
            Log("server ready");
            Application.ApplicationExit += delegate { StopServer(); };
            Application.Run(new MainForm(kw));
        }

        public static bool Ping()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + Port + "/api/ping");
                req.Proxy = null; req.Timeout = 2000;
                using (HttpWebResponse r = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(r.GetResponseStream()))
                    return sr.ReadToEnd().IndexOf("\"ok\":true") >= 0;
            }
            catch { return false; }
        }
        static void StopServer()
        {
            try { if (server != null && !server.HasExited) { server.Kill(); server.WaitForExit(3000); } } catch { }
        }
        public static void Log(string m)
        {
            try { File.AppendAllText(Path.Combine(Dir, "launch.log"), DateTime.Now.ToString("HH:mm:ss") + " " + m + "\r\n"); } catch { }
        }
    }

    class TreeCanvas : Panel
    {
        public Node root;
        public List<Node> flat = new List<Node>();
        public double yaw = 0.72, pitch = 0.38, zoom = 1.15;
        public bool autoRotate = true;
        public Node selected, hover;
        public Font fontSmall, fontNormal, fontBig;
        public event EventHandler SelectionChanged;

        static readonly double[] RAD = { 0, 125, 250, 370, 480 };
        static readonly double[] BY = { 170, 40, -150, -320, -480 };

        bool dragging = false, moved = false;
        int lastX, lastY;
        System.Windows.Forms.Timer timer;

        public TreeCanvas()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(15, 17, 21);
            fontSmall = new Font("Microsoft YaHei UI", 8f);
            fontNormal = new Font("Microsoft YaHei UI", 9f);
            fontBig = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 40;
            timer.Tick += delegate { if (autoRotate && !dragging) yaw += 0.0015; Invalidate(); };
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
                    n.x = rr * Math.Cos(n.angle);
                    n.y = BY[Math.Min(n.depth, 4)];
                    n.z = rr * Math.Sin(n.angle);
                }
            }
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
                case "app": return Color.FromArgb(255, 112, 67);
                case "dir": return Color.FromArgb(255, 213, 79);
                case "config": return Color.FromArgb(102, 187, 106);
                case "regkey": return Color.FromArgb(186, 104, 200);
                case "regvalue": return Color.FromArgb(206, 147, 216);
                case "lnk": return Color.FromArgb(79, 195, 247);
                case "proc": return Color.FromArgb(239, 83, 80);
                case "prefetch": return Color.FromArgb(120, 144, 156);
                case "cache": return Color.FromArgb(38, 198, 218);
                default: return Color.FromArgb(144, 164, 174);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try { PaintTree(e); } catch (Exception ex) { Program.Log("PAINT EX: " + ex.ToString()); }
        }
        void PaintTree(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            if (root == null || flat.Count == 0)
            {
                using (Font f = new Font("Microsoft YaHei UI", 11f))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("输入应用名并点「扫描」，或从左侧应用列表/手动路径加入\n\nL1 = 应用本体 exe\nL2 = 关联到它的全部内容\nL3 = 上一层内容的细节", f, Brushes.Gray,
                        new RectangleF(0, 0, Width, Height), sf);
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
                flat[i].sr = Math.Max(2.5f, (flat[i].depth == 1 ? 14f : flat[i].depth == 2 ? 9f : flat[i].depth == 3 ? 6.5f : 4f) * (c / 0.74f));
            }
            for (int i = 0; i < flat.Count; i++)
            {
                Node n = flat[i];
                if (n.parent == null) continue;
                int pi = flat.IndexOf(n.parent);
                if (pi < 0) continue;
                bool sel = (selected != null && (n == selected || n.parent == selected));
                int alpha = sel ? 230 : (n.depth <= 2 ? 100 : 50);
                using (Pen pen = new Pen(Color.FromArgb(alpha, 120, 140, 170), sel ? 1.6f : (n.depth <= 2 ? 1.1f : 0.7f)))
                    g.DrawLine(pen, px[pi], py[pi], px[i], py[i]);
            }
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
                if (isSel || isHov)
                {
                    int pad = isSel ? 6 : 4;
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(isSel ? 60 : 28, 255, isSel ? 213 : 255, isSel ? 79 : 255)))
                        g.FillEllipse(br, px[i] - r - pad, py[i] - r - pad, (r + pad) * 2, (r + pad) * 2);
                }
                using (SolidBrush br = new SolidBrush(col)) g.FillEllipse(br, px[i] - r, py[i] - r, r * 2, r * 2);
                using (Pen p = new Pen(isSel ? Color.FromArgb(255, 213, 79) : (isHov ? Color.White : Color.FromArgb(90, 0, 0, 0)), isSel ? 2.4f : 1f))
                    g.DrawEllipse(p, px[i] - r, py[i] - r, r * 2, r * 2);

                bool want = n.depth == 1 || isSel || isHov || (n.depth == 2 && zoom >= 0.8) || (n.depth == 3 && zoom >= 1.6);
                if (want)
                {
                    string txt = n.name.Length > 20 ? n.name.Substring(0, 19) + "…" : n.name;
                    Font f = n.depth == 1 ? fontBig : fontNormal;
                    SizeF sz = g.MeasureString(txt, f);
                    RectangleF rect = new RectangleF(px[i] + r + 5, py[i] - sz.Height / 2, sz.Width + 6, sz.Height + 2);
                    bool clash = false;
                    foreach (RectangleF rr in labels) if (rr.IntersectsWith(rect)) { clash = true; break; }
                    if (isSel || isHov || n.depth == 1 || !clash)
                    {
                        labels.Add(rect);
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(170, 10, 12, 16))) g.FillRectangle(b, rect);
                        Color tc = isSel ? Color.FromArgb(255, 224, 130) : (n.depth == 1 ? Color.FromArgb(255, 215, 194) : (n.depth == 2 ? Color.FromArgb(201, 212, 226) : Color.FromArgb(139, 147, 163)));
                        using (SolidBrush tb = new SolidBrush(tc)) g.DrawString(txt, f, tb, rect.X + 3, rect.Y + 1);
                    }
                }
            }
            DrawLegend(g);
        }

        void DrawLegend(Graphics g)
        {
            string[] kinds = { "app", "dir", "config", "regkey", "lnk", "proc", "file" };
            string[] labels = { "L1 应用本体(exe)", "L2 目录", "配置", "注册表", "快捷方式", "进程", "文件" };
            int x = 12, y = 10;
            Font f = fontSmall;
            for (int i = 0; i < kinds.Length; i++)
            {
                using (SolidBrush b = new SolidBrush(KindColor(kinds[i]))) g.FillEllipse(b, x, y + 3, 8, 8);
                g.DrawString(labels[i], f, Brushes.Gray, x + 12, y);
                x += (int)g.MeasureString(labels[i], f).Width + 34;
            }
            g.DrawString("拖动旋转 · 滚轮缩放 · 点击选中", f, Brushes.DimGray, 12, y + 22);
        }

        Node Pick(int mx, int my)
        {
            Node best = null; double bd = double.MaxValue;
            foreach (Node n in flat)
            {
                double dx = n.sx - mx, dy = n.sy - my;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d < n.sr + 12 && d < bd) { bd = d; best = n; }
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
                yaw += dx * 0.007;
                pitch = Math.Max(-1.25, Math.Min(1.25, pitch + dy * 0.007));
                lastX = e.X; lastY = e.Y;
                Invalidate();
            }
            else
            {
                Node n = Pick(e.X, e.Y);
                if (n != hover) { hover = n; Invalidate(); }
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
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            zoom = Math.Max(0.35, Math.Min(3.2, zoom * (float)Math.Exp(-e.Delta * 0.0012)));
            Invalidate();
            base.OnMouseWheel(e);
        }
        protected override void OnResize(EventArgs eventargs)
        {
            Invalidate();
            base.OnResize(eventargs);
        }
    }
}
