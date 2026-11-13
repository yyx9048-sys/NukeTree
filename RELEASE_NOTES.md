# NukeTree 1.0.0 — Windows 3D 应用彻底清理

首个打包发布版本。原生 WinForms + 本地 PowerShell 后端，无需浏览器内核。

## 下载

| 文件 | 说明 | SHA256 |
|---|---|---|
| `NukeTree-1.0.0-win64.zip` | **绿色便携版**，解压双击即用 | `E97DC17B2E428801E5C979FEB688E04DD648AAF9EBBF1AD58347D8DBCA82F5A8` |
| `NukeTree-1.0.0-win64-setup.zip` | **安装版**（解压后运行 `安装.cmd`，装到 `%LOCALAPPDATA%\NukeTree` 并建开始菜单快捷方式，可一键卸载） | `F7F388FF4C81527DE82854F513848A86DC6E25242E1E613B8A4423113106F8DB` |

也可通过 **npm** 安装（包内含预编译 exe，仅 Windows x64）：

```powershell
npm install -g nuketree
nuketree
# 或
npx nuketree
```

npm 包文件：`nuketree-1.0.0.tgz`（仓库 `release/` 目录下，已按 npm 格式打包 `package/` 布局）

校验：

```powershell
Get-FileHash .\NukeTree-1.0.0-win64.zip -Algorithm SHA256
```

## 亮点

- **3D 依赖树**：应用/目录/配置/注册表/快捷方式/进程/Prefetch 统一建模；放射状不规则直线、球体防重叠、拖拽旋转、滚轮/按钮缩放
- **全盘总览**：按磁盘聚合整机文件，双击逐层展开，不铺满成乱麻
- **拖拽加入**：从资源管理器把文件/文件夹拖进 3D 画布
- **3D 内查找**：按名称/路径搜索并高亮
- **彻底删除 + 验收**：删除前备份（文件 + 注册表 `.reg`）；删除后显示磁盘回收空间 + 残留检测（注册表/服务/计划任务/启动项/Prefetch/AppData·ProgramData 目录）
- **共用依赖检测**：标出被其它应用/系统共用的 DLL（如 `UnityPlayer.dll`、`chrome_elf.dll`），避免误删
- **AI 文件作用提示**：悬停 3D 球体或文件行显示作用；未配置时走内置规则库，可选接 OpenAI 兼容 API 或同步 DSH 配置
- **插件系统**：`plugins/<id>/plugin.json` + 脚本/exe，可对选中节点运行，支持启用/停用；内置 `file-hash`、`reveal` 示例
- **个性化**：背景预设/自定义背景色、标注色；暗色标题栏；字体与 DSH 一致

## 系统要求

- Windows 10 / 11 x64
- .NET Framework 4.x（系统自带）
- PowerShell 5.1（系统自带）
- 删除系统目录/服务/计划任务时需管理员权限

## 安装 / 使用

**便携版**

1. 解压 `NukeTree-1.0.0-win64.zip` 到任意目录
2. 双击 `NukeTree.exe`
3. 首次启动自动释放 `plugins\` 目录

**安装版**

1. 解压 `NukeTree-1.0.0-win64-setup.zip`
2. 运行 `安装.cmd`（无需管理员）
3. 从开始菜单启动；卸载运行安装目录里的 `卸载.cmd`

## 安全说明

- 删除前自动备份到 `备份3D\del_<时间>\`，可恢复文件与注册表
- 受保护路径（`C:\Windows`、`.dsh`、`Common Files`、`WindowsApps` 等）自动跳过
- 本工具本身不联网；AI 功能仅在你主动配置 API 后请求指定服务
- 误删系统组件可能影响系统稳定，请确认目标后再删除

## 已知说明

- 卸载软件的"磁盘恢复到安装前大小"无法做到 100%（系统 Prefetch、事件日志、卷影副本不归应用管理）；本版本会显示实际回收空间与残留清单，残留可再次加入队列删除
- 极少数系统上后端 `TcpListener` 端口 18765 被占用时会启动失败，关闭占用进程后重试
