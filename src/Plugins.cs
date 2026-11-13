using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AppNuke3D
{
    class PluginItem
    {
        public string Id = "";
        public string Name = "";
        public string Version = "";
        public string Description = "";
        public string Author = "";
        public string Command = "";
        public string Args = "";
        public string Dir = "";
        public bool Enabled = true;
    }

    static class Plugins
    {
        public static string Dir { get { return Path.Combine(Program.Dir, "plugins"); } }
        static string StateFile { get { return Path.Combine(Dir, "state.json"); } }

        public static void EnsureSamples()
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                string hashDir = Path.Combine(Dir, "file-hash");
                if (!Directory.Exists(hashDir))
                {
                    Directory.CreateDirectory(hashDir);
                    File.WriteAllText(Path.Combine(hashDir, "plugin.json"),
                        "{\"name\":\"文件哈希校验\",\"version\":\"1.0\",\"description\":\"计算所选文件的 SHA256 / 大小 / 修改时间\",\"author\":\"NukeTree\",\"command\":\"run.ps1\",\"args\":\"\\\"{path}\\\"\"}",
                        new UTF8Encoding(false));
                    File.WriteAllText(Path.Combine(hashDir, "run.ps1"),
                        "param([string]$Path)\r\n" +
                        "if (-not (Test-Path -LiteralPath $Path)) { Write-Output '路径不存在'; exit }\r\n" +
                        "$i = Get-Item -LiteralPath $Path\r\n" +
                        "Write-Output ('名称: ' + $i.Name)\r\n" +
                        "Write-Output ('大小: ' + [math]::Round($i.Length / 1MB, 3) + ' MB (' + $i.Length + ' 字节)')\r\n" +
                        "Write-Output ('修改: ' + $i.LastWriteTime)\r\n" +
                        "if (-not $i.PSIsContainer) {\r\n" +
                        "  $h = Get-FileHash -LiteralPath $Path -Algorithm SHA256\r\n" +
                        "  Write-Output ('SHA256: ' + $h.Hash)\r\n" +
                        "}\r\n",
                        new UTF8Encoding(true));
                }
                string revealDir = Path.Combine(Dir, "reveal");
                if (!Directory.Exists(revealDir))
                {
                    Directory.CreateDirectory(revealDir);
                    File.WriteAllText(Path.Combine(revealDir, "plugin.json"),
                        "{\"name\":\"在资源管理器中定位\",\"version\":\"1.0\",\"description\":\"打开所在文件夹并选中该文件\",\"author\":\"NukeTree\",\"command\":\"explorer.exe\",\"args\":\"/select,\\\"{path}\\\"\"}",
                        new UTF8Encoding(false));
                }
            }
            catch (Exception ex) { Program.Log("plugin sample err " + ex.Message); }
        }

        static Dictionary<string, bool> LoadState()
        {
            Dictionary<string, bool> st = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(StateFile))
                {
                    Dictionary<string, object> d = Json.Parse(File.ReadAllText(StateFile, Encoding.UTF8));
                    foreach (KeyValuePair<string, object> kv in d)
                        st[kv.Key] = Convert.ToBoolean(kv.Value);
                }
            }
            catch { }
            return st;
        }

        public static void SaveState(List<PluginItem> list)
        {
            try
            {
                StringBuilder sb = new StringBuilder("{");
                bool first = true;
                foreach (PluginItem p in list)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append("\"" + p.Id.Replace("\"", "") + "\":" + (p.Enabled ? "true" : "false"));
                }
                sb.Append("}");
                File.WriteAllText(StateFile, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception ex) { Program.Log("plugin state err " + ex.Message); }
        }

        public static List<PluginItem> Load()
        {
            EnsureSamples();
            List<PluginItem> list = new List<PluginItem>();
            Dictionary<string, bool> st = LoadState();
            try
            {
                foreach (string d in Directory.GetDirectories(Dir))
                {
                    string mf = Path.Combine(d, "plugin.json");
                    if (!File.Exists(mf)) continue;
                    try
                    {
                        Dictionary<string, object> d0 = Json.Parse(File.ReadAllText(mf, Encoding.UTF8));
                        PluginItem p = new PluginItem();
                        p.Dir = d;
                        p.Id = Path.GetFileName(d);
                        p.Name = Get(d0, "name", p.Id);
                        p.Version = Get(d0, "version", "");
                        p.Description = Get(d0, "description", "");
                        p.Author = Get(d0, "author", "");
                        p.Command = Get(d0, "command", "");
                        p.Args = Get(d0, "args", "");
                        p.Enabled = st.ContainsKey(p.Id) ? st[p.Id] : true;
                        list.Add(p);
                    }
                    catch (Exception ex) { Program.Log("plugin " + d + " err " + ex.Message); }
                }
            }
            catch (Exception ex) { Program.Log("plugins err " + ex.Message); }
            list.Sort(delegate (PluginItem a, PluginItem b) { return string.Compare(a.Name, b.Name, StringComparison.CurrentCulture); });
            return list;
        }

        static string Get(Dictionary<string, object> d, string k, string def)
        {
            return (d.ContainsKey(k) && d[k] != null) ? Convert.ToString(d[k]) : def;
        }

        public static string Run(PluginItem p, string path, string name)
        {
            if (p == null) return "未选择插件";
            if (p.Command.Length == 0) return "该插件没有配置 command";
            string args = (p.Args ?? "").Replace("{path}", path ?? "").Replace("{name}", name ?? "");
            try
            {
                ProcessStartInfo psi;
                if (p.Command.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    string script = Path.IsPathRooted(p.Command) ? p.Command : Path.Combine(p.Dir, p.Command);
                    psi = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" " + args);
                }
                else if (p.Command.IndexOf('\\') >= 0 || p.Command.IndexOf('/') >= 0)
                {
                    string exe = Path.IsPathRooted(p.Command) ? p.Command : Path.Combine(p.Dir, p.Command);
                    psi = new ProcessStartInfo(exe, args);
                }
                else
                {
                    psi = new ProcessStartInfo(p.Command, args);
                }
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process pr = Process.Start(psi))
                {
                    if (pr == null) return "无法启动插件进程";
                    string output = "";
                    try
                    {
                        output = pr.StandardOutput.ReadToEnd();
                        string err = pr.StandardError.ReadToEnd();
                        if (!string.IsNullOrEmpty(err)) output += (output.Length > 0 ? "\r\n" : "") + err;
                    }
                    catch { }
                    if (!pr.WaitForExit(20000))
                    {
                        try { pr.Kill(); } catch { }
                        return "插件超时（20 秒）已终止\r\n" + output;
                    }
                    if (output.Length > 4000) output = output.Substring(0, 4000) + "\r\n…";
                    if (output.Trim().Length == 0) output = "插件已执行（退出码 " + pr.ExitCode + "，无输出）";
                    return output;
                }
            }
            catch (Exception ex) { return "插件执行失败: " + ex.Message; }
        }
    }
}
