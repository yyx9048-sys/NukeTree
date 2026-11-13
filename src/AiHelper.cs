using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace AppNuke3D
{
    static class AiHelper
    {
        static Dictionary<string, string> cache = new Dictionary<string, string>();
        static object gate = new object();

        public static string LocalDescribe(string name, string path, bool isDir)
        {
            string ext = "";
            try { ext = Path.GetExtension(name).ToLower(); } catch { }
            if (isDir)
            {
                string lowD = (name ?? "").ToLower();
                if (lowD == "windows" || lowD == "system32") return "Windows 系统目录，存放系统组件与核心程序。";
                if (lowD == "users") return "用户账户目录，包含桌面/文档/AppData 等个人数据。";
                if (lowD.StartsWith("program files")) return "软件安装目录，程序本体通常装在这里。";
                if (lowD == "appdata" || lowD == "roaming" || lowD == "local" || lowD == "locallow") return "程序的配置与缓存数据目录。";
                if (lowD.Contains("temp") || lowD == "tmp") return "临时文件目录，程序运行产生的临时数据。";
                if (lowD.Contains("cache")) return "缓存目录，用于加速访问，通常可清理。";
                if (lowD.Contains("config") || lowD.Contains("setting")) return "配置目录，保存软件设置。";
                if (lowD.Contains("log")) return "日志目录，记录运行信息。";
                if (lowD.Contains("download")) return "下载目录，存放下载的文件。";
                return "文件夹，内部包含子文件与子目录。";
            }
            switch (ext)
            {
                case ".exe": return "可执行程序，双击即可运行。";
                case ".dll": return "动态链接库，供程序调用的代码模块。";
                case ".sys": return "系统驱动文件。";
                case ".ini": case ".cfg": case ".conf": return "配置文件，保存程序参数设置。";
                case ".json": case ".jsonc": return "数据/配置文件（JSON 格式）。";
                case ".yml": case ".yaml": return "配置文件（YAML 格式）。";
                case ".toml": return "配置文件（TOML 格式）。";
                case ".xml": return "数据/配置文件（XML 格式）。";
                case ".log": return "日志文件，记录运行信息。";
                case ".db": case ".sqlite": return "数据库文件，保存程序数据。";
                case ".pak": case ".dat": case ".bin": return "程序的资源/数据文件。";
                case ".lnk": return "快捷方式，指向另一个程序或文件。";
                case ".zip": case ".7z": case ".rar": return "压缩包。";
                case ".png": case ".jpg": case ".jpeg": case ".webp": case ".gif": case ".bmp": return "图片文件。";
                case ".mp4": case ".mkv": case ".mp3": case ".wav": case ".flac": return "音视频文件。";
                case ".txt": case ".md": return "文本文件。";
                case ".msi": return "安装包（Windows Installer）。";
                case ".ttf": case ".otf": return "字体文件。";
            }
            string low = (name ?? "").ToLower();
            if (low.Contains("unins")) return "卸载程序，用于移除对应软件。";
            if (low.Contains("license") || low.Contains("licence")) return "许可/授权文件。";
            if (low.Contains("readme")) return "说明文档。";
            if (low.Contains("config") || low.Contains("setting")) return "配置文件。";
            if (low.Contains("cache")) return "缓存文件。";
            if (low.Contains("log")) return "日志文件。";
            return "普通文件。";
        }

        public static void DescribeAsync(string name, string path, bool isDir, Action<string> done)
        {
            string key = string.IsNullOrEmpty(path) ? name : path;
            lock (gate) { if (cache.ContainsKey(key)) { done(cache[key]); return; } }
            AppSettings s = SettingsStore.Load();
            if (!s.AiEnabled || string.IsNullOrEmpty(s.ApiKey)) return;
            Thread t = new Thread(delegate ()
            {
                string result = null;
                try
                {
                    string url = s.ApiBase.TrimEnd('/') + "/chat/completions";
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Proxy = null;
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.Timeout = 20000;
                    req.Headers.Add("Authorization", "Bearer " + s.ApiKey);
                    string prompt = "请用一句简短中文（40 字内）说明这个 Windows " + (isDir ? "文件夹" : "文件") + "的作用，只回答作用：" + (string.IsNullOrEmpty(path) ? name : path);
                    string body = "{\"model\":\"" + Escape(s.ApiModel) + "\",\"messages\":[{\"role\":\"user\",\"content\":\"" + Escape(prompt) + "\"}],\"max_tokens\":80}";
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    req.ContentLength = data.Length;
                    using (Stream st = req.GetRequestStream()) st.Write(data, 0, data.Length);
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string txt = sr.ReadToEnd();
                        Dictionary<string, object> d = Json.Parse(txt);
                        ArrayList choices = (ArrayList)d["choices"];
                        Dictionary<string, object> c0 = (Dictionary<string, object>)choices[0];
                        Dictionary<string, object> msg = (Dictionary<string, object>)c0["message"];
                        result = Convert.ToString(msg["content"]).Trim();
                    }
                    if (!string.IsNullOrEmpty(result))
                    {
                        lock (gate) { cache[key] = result; }
                    }
                }
                catch (Exception ex) { Program.Log("ai hover err " + ex.Message); }
                if (result != null) { try { done(result); } catch { } }
            });
            t.IsBackground = true; t.Start();
        }

        static string Escape(string s)
        {
            return (s == null ? "" : s).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
        }
    }
}
