using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AppNuke3D
{
    class AppSettings
    {
        public bool AiEnabled { get; set; }
        public string ApiBase { get; set; }
        public string ApiModel { get; set; }
        public string ApiKey { get; set; }
        public string BgTop { get; set; }
        public string BgBottom { get; set; }
        public string Accent { get; set; }

        public AppSettings()
        {
            AiEnabled = false;
            ApiBase = "https://api.openai.com/v1";
            ApiModel = "gpt-4o-mini";
            ApiKey = "";
            BgTop = "#202326";
            BgBottom = "#15171A";
            Accent = "#D9DCE0";
        }
    }

    static class SettingsStore
    {
        public static string FilePath { get { return Path.Combine(Program.Dir, "settings.json"); } }

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                if (File.Exists(FilePath))
                {
                    Dictionary<string, object> d = Json.Parse(File.ReadAllText(FilePath, Encoding.UTF8));
                    s.AiEnabled = GetBool(d, "AiEnabled");
                    s.ApiBase = GetStr(d, "ApiBase", s.ApiBase);
                    s.ApiModel = GetStr(d, "ApiModel", s.ApiModel);
                    s.ApiKey = GetStr(d, "ApiKey", s.ApiKey);
                    s.BgTop = GetStr(d, "BgTop", s.BgTop);
                    s.BgBottom = GetStr(d, "BgBottom", s.BgBottom);
                    s.Accent = GetStr(d, "Accent", s.Accent);
                }
            }
            catch (Exception ex) { Program.Log("settings load err " + ex.Message); }
            return s;
        }
        public static void Save(AppSettings s)
        {
            try { File.WriteAllText(FilePath, Json.Stringify(s), Encoding.UTF8); }
            catch (Exception ex) { Program.Log("settings save err " + ex.Message); }
        }
        static string GetStr(Dictionary<string, object> d, string k, string def)
        {
            return (d.ContainsKey(k) && d[k] != null) ? Convert.ToString(d[k]) : def;
        }
        static bool GetBool(Dictionary<string, object> d, string k)
        {
            if (!d.ContainsKey(k) || d[k] == null) return false;
            try { return Convert.ToBoolean(d[k]); } catch { return false; }
        }
    }

    class SettingsForm : Form
    {
        CheckBox chkAi;
        TextBox tbBase, tbModel, tbKey;
        Label lblAiStatus;
        ComboBox cmbPreset;
        Button btnBgTop, btnBgBottom, btnAccent, btnDefault;
        Color bgTop, bgBottom, accent;
        AppSettings cur;

        public SettingsForm(AppSettings s)
        {
            cur = s;
            bgTop = Parse(s.BgTop, Color.FromArgb(32, 35, 38));
            bgBottom = Parse(s.BgBottom, Color.FromArgb(21, 23, 26));
            accent = Parse(s.Accent, Color.FromArgb(217, 220, 224));

            Text = "设置 — AI 接入 / 个性化";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(760, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = Theme.Bg; ForeColor = Theme.Txt;
            Font = new Font("Segoe UI", 9f);

            Label h = new Label(); h.Text = "设置"; h.Font = new Font("Segoe UI", 14f, FontStyle.Bold); h.AutoSize = true; h.Location = new Point(22, 16);
            Controls.Add(h);

            // ---- AI ----
            Label a = Section("AI 接入", 22, 60);
            chkAi = new CheckBox(); chkAi.Text = "启用 AI（按配置的 API 调用）"; chkAi.AutoSize = true; chkAi.Location = new Point(30, 92); chkAi.ForeColor = Theme.Txt; chkAi.Checked = s.AiEnabled;
            Controls.Add(chkAi);
            Controls.Add(LabelAt("API Base", 30, 126)); tbBase = TextBoxAt(30, 146); tbBase.Text = s.ApiBase; Controls.Add(tbBase);
            Controls.Add(LabelAt("模型 Model", 30, 180)); tbModel = TextBoxAt(30, 200); tbModel.Text = s.ApiModel; Controls.Add(tbModel);
            Controls.Add(LabelAt("API Key", 380, 180)); tbKey = TextBoxAt(380, 200); tbKey.Text = s.ApiKey; tbKey.UseSystemPasswordChar = true; Controls.Add(tbKey);
            Button btnSync = ButtonAt("从 DSH 同步配置", 30, 236, 150);
            btnSync.Click += delegate { SyncFromDsh(); };
            Controls.Add(btnSync);
            Button btnTest = ButtonAt("测试连接", 190, 236, 100);
            btnTest.Click += delegate { TestAi(); };
            Controls.Add(btnTest);
            lblAiStatus = LabelAt("", 30, 272); lblAiStatus.Width = 690; Controls.Add(lblAiStatus);
            if (s.AiEnabled) lblAiStatus.Text = "AI 已启用（配置来自上次保存）";

            // ---- 个性化 ----
            Label t = Section("个性化 · 背景与标注颜色", 22, 306);
            Controls.Add(LabelAt("背景预设", 30, 338));
            cmbPreset = new ComboBox();
            cmbPreset.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPreset.Location = new Point(112, 334); cmbPreset.Width = 200;
            cmbPreset.Items.AddRange(new object[] { "深灰（默认）", "纯黑", "深蓝", "暖褐", "自定义" });
            cmbPreset.SelectedIndex = 0;
            cmbPreset.SelectedIndexChanged += delegate { ApplyPreset(); };
            Controls.Add(cmbPreset);

            btnBgTop = ColorButton("背景顶部", 30, 376, bgTop);
            btnBgTop.Click += delegate { Pick(ref bgTop, btnBgTop); };
            Controls.Add(btnBgTop);
            btnBgBottom = ColorButton("背景底部", 190, 376, bgBottom);
            btnBgBottom.Click += delegate { Pick(ref bgBottom, btnBgBottom); };
            Controls.Add(btnBgBottom);
            btnAccent = ColorButton("标注颜色", 350, 376, accent);
            btnAccent.Click += delegate { Pick(ref accent, btnAccent); };
            Controls.Add(btnAccent);
            btnDefault = ButtonAt("恢复默认", 510, 376, 100);
            btnDefault.Click += delegate
            {
                bgTop = Color.FromArgb(32, 35, 38); bgBottom = Color.FromArgb(21, 23, 26); accent = Color.FromArgb(217, 220, 224);
                SetSwatch(btnBgTop, bgTop); SetSwatch(btnBgBottom, bgBottom); SetSwatch(btnAccent, accent);
                cmbPreset.SelectedIndex = 0;
            };
            Controls.Add(btnDefault);

            Label tip = LabelAt("提示：标注颜色用于 3D 选中框、标签描边、强调线；保存后立即生效。", 30, 414); tip.Width = 700; tip.ForeColor = Theme.Dim;
            Controls.Add(tip);

            // ---- 按钮 ----
            Button ok = ButtonAt("保存", 540, 500, 90); ok.Click += delegate { SaveAndClose(); };
            Button cancel = ButtonAt("取消", 640, 500, 90); cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(ok); Controls.Add(cancel);

            ApplyRaw();
            // 高 DPI 缩放
            float k = 1f;
            try { using (Graphics dg = CreateGraphics()) k = dg.DpiX / 96f; } catch { }
            if (k > 1.01f)
            {
                ClientSize = new Size((int)(ClientSize.Width * k), (int)(ClientSize.Height * k));
                foreach (Control c in Controls)
                    c.Bounds = new Rectangle((int)(c.Left * k), (int)(c.Top * k), (int)(c.Width * k), (int)(c.Height * k));
            }
        }

        Label Section(string text, int x, int y)
        {
            Label l = new Label(); l.Text = text; l.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold); l.ForeColor = Theme.Txt; l.AutoSize = true; l.Location = new Point(x, y);
            return l;
        }
        Label LabelAt(string text, int x, int y)
        {
            Label l = new Label(); l.Text = text; l.ForeColor = Theme.Dim; l.AutoSize = true; l.Location = new Point(x, y);
            return l;
        }
        TextBox TextBoxAt(int x, int y)
        {
            TextBox t = new TextBox(); t.BackColor = Theme.Panel; t.ForeColor = Theme.Txt; t.BorderStyle = BorderStyle.FixedSingle;
            t.Location = new Point(x, y); t.Width = 320;
            return t;
        }
        Button ButtonAt(string text, int x, int y, int w)
        {
            Button b = new Button();
            b.Text = text; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Theme.Line;
            b.BackColor = Theme.Panel; b.ForeColor = Theme.Txt;
            b.SetBounds(x, y, w, 30);
            return b;
        }
        Button ColorButton(string text, int x, int y, Color c)
        {
            Button b = ButtonAt(text, x, y, 140);
            SetSwatch(b, c);
            return b;
        }
        void SetSwatch(Button b, Color c)
        {
            b.BackColor = c;
            b.ForeColor = c.GetBrightness() > 0.55f ? Color.Black : Color.White;
        }
        void Pick(ref Color target, Button b)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.FullOpen = true; cd.Color = target;
                if (cd.ShowDialog() == DialogResult.OK) { target = cd.Color; SetSwatch(b, target); }
            }
        }
        void ApplyPreset()
        {
            switch (cmbPreset.SelectedIndex)
            {
                case 1: bgTop = Color.FromArgb(24, 24, 24); bgBottom = Color.FromArgb(8, 8, 8); break;
                case 2: bgTop = Color.FromArgb(22, 30, 44); bgBottom = Color.FromArgb(10, 14, 22); break;
                case 3: bgTop = Color.FromArgb(40, 34, 28); bgBottom = Color.FromArgb(18, 15, 12); break;
                default: bgTop = Color.FromArgb(32, 35, 38); bgBottom = Color.FromArgb(21, 23, 26); break;
            }
            SetSwatch(btnBgTop, bgTop); SetSwatch(btnBgBottom, bgBottom);
        }
        void ApplyRaw() { }

        void SaveAndClose()
        {
            cur.AiEnabled = chkAi.Checked;
            cur.ApiBase = tbBase.Text.Trim();
            cur.ApiModel = tbModel.Text.Trim();
            cur.ApiKey = tbKey.Text.Trim();
            cur.BgTop = Hex(bgTop); cur.BgBottom = Hex(bgBottom); cur.Accent = Hex(accent);
            SettingsStore.Save(cur);
            DialogResult = DialogResult.OK;
            Close();
        }
        void TestAi()
        {
            lblAiStatus.ForeColor = Theme.Dim;
            lblAiStatus.Text = "正在测试…";
            string baseUrl = tbBase.Text.Trim().TrimEnd('/');
            string key = tbKey.Text.Trim();
            if (baseUrl.Length == 0) { lblAiStatus.Text = "请填写 API Base"; return; }
            string status = "";
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    System.Net.ServicePointManager.Expect100Continue = false;
                    var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(baseUrl + "/models");
                    req.Proxy = null; req.Timeout = 8000;
                    if (key.Length > 0) req.Headers.Add("Authorization", "Bearer " + key);
                    using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string body = sr.ReadToEnd();
                        status = "连接成功（HTTP " + (int)resp.StatusCode + "），返回 " + body.Length + " 字节";
                    }
                }
                catch (Exception ex) { status = "连接失败: " + ex.Message; }
                try { BeginInvoke((MethodInvoker)delegate { lblAiStatus.ForeColor = status.StartsWith("连接成功") ? Color.FromArgb(120, 200, 120) : Color.FromArgb(230, 120, 120); lblAiStatus.Text = status; }); } catch { }
            });
        }
        void SyncFromDsh()
        {
            string dshDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dsh");
            string baseUrl = "", model = "", key = "";
            List<string> hitFiles = new List<string>();
            try
            {
                if (Directory.Exists(dshDir))
                {
                    List<string> files = new List<string>(Directory.GetFiles(dshDir, "*.*", SearchOption.AllDirectories));
                    int n = 0;
                    foreach (string f in files)
                    {
                        if (n++ > 400) break;
                        string ext = Path.GetExtension(f).ToLower();
                        if (ext != ".json" && ext != ".yml" && ext != ".yaml" && ext != ".toml" && ext != ".jsonc") continue;
                        try
                        {
                            string txt = File.ReadAllText(f, Encoding.UTF8);
                            if (txt.IndexOf("model", StringComparison.OrdinalIgnoreCase) < 0 && txt.IndexOf("api", StringComparison.OrdinalIgnoreCase) < 0) continue;
                            if (baseUrl.Length == 0)
                            {
                                Match m = Regex.Match(txt, "(?i)(base[_\\-]?url|api[_\\-]?base)\\s*[:=]\\s*[\"']?(https?://[^\\s\"',}]+)");
                                if (m.Success) { baseUrl = m.Groups[2].Value; hitFiles.Add(Path.GetFileName(f) + " (base)"); }
                            }
                            if (model.Length == 0)
                            {
                                Match m = Regex.Match(txt, "(?i)[\"']?(model|modelId|model_id)[\"']?\\s*[:=]\\s*[\"']?([A-Za-z0-9_.:/-]{2,})");
                                if (m.Success) { model = m.Groups[2].Value; hitFiles.Add(Path.GetFileName(f) + " (model)"); }
                            }
                            if (key.Length == 0)
                            {
                                Match m = Regex.Match(txt, "(sk-[A-Za-z0-9_-]{10,})");
                                if (m.Success) { key = m.Groups[1].Value; hitFiles.Add(Path.GetFileName(f) + " (key)"); }
                            }
                            if (baseUrl.Length > 0 && model.Length > 0 && key.Length > 0) break;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex) { lblAiStatus.Text = "同步失败: " + ex.Message; return; }
            if (baseUrl.Length > 0) tbBase.Text = baseUrl;
            if (model.Length > 0) tbModel.Text = model;
            if (key.Length > 0) tbKey.Text = key;
            if (hitFiles.Count == 0) lblAiStatus.Text = "未在 DSH 目录找到配置（可手动填写 API Base / Model / Key）";
            else lblAiStatus.Text = "已同步 DSH 配置：" + string.Join(", ", hitFiles.ToArray()).ToString();
        }
        static Color Parse(string s, Color def)
        {
            try
            {
                if (string.IsNullOrEmpty(s)) return def;
                return ColorTranslator.FromHtml(s);
            }
            catch { return def; }
        }
        static string Hex(Color c) { return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2"); }
    }
}
