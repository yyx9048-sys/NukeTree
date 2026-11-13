# 如何发布 NukeTree

面向两个渠道：**GitHub（源码 + Release 安装包）** 和 **npm（`npx nuketree`）**。

> 本仓库已内置 CI：推 tag 自动构建并创建 Release；Release 发布后自动 `npm publish`。
> 你只需要准备账号 + 两个 secret。

---

## 0. 准备

在**你自己的终端**（Windows PowerShell 或 Git Bash）里操作，不要在本 AI 沙箱里执行。

需要：

- [Git for Windows](https://git-scm.com/download/win)（含 `git`）
- Node.js 18+ / npm（`node -v`、`npm -v` 能输出版本号）
- GitHub 账号、[npmjs.com](https://www.npmjs.com/signup) 账号

配置 Git 身份（只需一次）：

```powershell
git config --global user.name "你的名字"
git config --global user.email "you@example.com"
```

---

## 1. 把源码推到 GitHub

1) 在 GitHub 网页新建仓库：**New repository** → 名称 `NukeTree` → Public → **不要**勾选 README/.gitignore/License → Create。
2) 在本项目目录执行（把 `<你的账号>` 换成你的 GitHub 用户名）：

```powershell
cd "D:\deepseek Harness\NukeTree"

git init -b main
git add .
git commit -m "feat: NukeTree 1.0.0 - 3D 应用彻底清理（插件/AI/共用依赖检测）"
git remote add origin https://github.com/<你的账号>/NukeTree.git
git push -u origin main
```

> 仓库里 `.gitignore` 已忽略 `dist/`、`*.exe` 等构建产物，只上传源码；exe 由 CI/Release 提供。

建议在仓库 **Settings → Topics** 添加：`windows` `winforms` `3d` `dependency` `uninstaller` `cleanup` `powershell`。

---

## 2. 打 tag，自动生成 Release（推荐）

```powershell
git tag v1.0.0
git push origin v1.0.0
```

推送后 GitHub Actions 自动执行 `.github/workflows/build-release.yml`：

- Windows runner 编译 `dist/NukeTree.exe`
- 生成 `NukeTree-1.0.0-win64.zip` + `.sha256.txt`
- 创建 GitHub Release 并上传这两个文件

到仓库 **Actions** 页面确认运行成功，然后 **Releases** 里就能下载。

### 手动上传 Release（不想用 Actions 时）

```powershell
# 先本地打包（脚本会自动 build + 生成 zip/sha256）
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\make-release.ps1 -Version 1.0.0
```

然后 GitHub → Releases → **Draft a new release** → 选 tag `v1.0.0` → 上传 `release/` 下的 `*.zip` 和 `*.sha256.txt` → Publish。

---

## 3. 发布到 npm

### 3.1 注册 / 登录

```powershell
npm login          # 交互式输入用户名、密码、邮箱（开了 2FA 会要 OTP）
npm whoami         # 确认已登录
```

> 包名 `nuketree` 目前未被占用。若以后被占用，把 `package.json` 的 `name` 改成 `nuketree-3d` 或 `@<你的账号>/nuketree`。

### 3.2 发布（在 Windows 上执行）

```powershell
cd "D:\deepseek Harness\NukeTree"
npm publish --access public
```

`prepack` 会自动调用 `build.ps1` 编译 exe（需要系统自带 `csc.exe` / .NET Framework 4.x）。

**或者直接发布已打好的 tarball**（不含源码，只有运行时文件）：

```powershell
npm publish .\release\nuketree-1.0.0.tgz --access public
```

### 3.3 验证

```powershell
npm view nuketree version
npx nuketree@1.0.0
```

### 3.4 用 GitHub Actions 自动发布 npm

1. 在 npmjs.com → **Access Tokens** → Generate New Token → **Automation** → 复制。
2. GitHub 仓库 → **Settings → Secrets and variables → Actions → New repository secret**
   - Name: `NPM_TOKEN`
   - Value: 刚才的 token
3. 之后每次在 GitHub 发布 Release，`publish-npm.yml` 会自动 `npm publish`。

---

## 4. 发布到 GitHub Packages（npm.pkg.github.com，可选）

GitHub Packages 要求包名带作用域：

```json
"name": "@<你的账号>/nuketree"
```

在项目根新建 `.npmrc`：

```ini
@<你的账号>:registry=https://npm.pkg.github.com
```

发布：

```powershell
npm publish --registry=https://npm.pkg.github.com
```

CI 里可用 `GITHUB_TOKEN`（无需额外 secret）。注意：GitHub Packages 的包安装时也要求登录带 `read:packages` 权限的 token，普通用户用 `npx` 不如 npmjs 方便。

---

## 5. 版本升级流程

```powershell
# 1) 改代码
# 2) 更新版本号（会自动改 package.json）
npm version patch   # 或 minor / major
# 3) 提交并打 tag
git add .
git commit -m "chore: release v1.0.1"
git push
git push --tags
# 4) 发布 npm
npm publish --access public
```

---

## 6. 常见问题

| 问题 | 处理 |
|---|---|
| `npm publish` 报 403 / 需要 2FA | 用 **Automation** token 作为 `NODE_AUTH_TOKEN`，或在交互终端发布 |
| `EPERM` / 权限不足 | 不要在 `C:\Program Files` 下发布；项目放在用户目录 |
| `prepack` 执行失败 | 确认 `powershell` 可用；或在 Windows 上先手动 `.\build.ps1`，再 `npm publish --ignore-scripts`（需已存在 `dist/`） |
| 包名被占用 | 改 `package.json` 的 `name`，或加作用域 |
| GitHub Actions 构建失败 | 看 Actions 日志；常见是 `.ps1` 编码（必须 UTF-8 **带 BOM**）或标签名不是 `v*` |
| 想撤包 | `npm unpublish nuketree@1.0.0`（24 小时内可删版本；超过需联系 npm） |

---

## 7. 发布前检查清单

- [ ] `build.ps1` 在本机构建成功，`dist\NukeTree.exe` 能双击运行
- [ ] `release\` 里的 zip 包能解压运行
- [ ] `package.json` 的 `version` 与 tag 一致
- [ ] `.ps1` 文件均为 **UTF-8 with BOM**（否则中文会乱码/语法错误）
- [ ] `README.md` 截图路径正确（`screenshots/...`）
- [ ] 不提交 `settings.json`、`备份3D/`、`dist/`、`launch.log`
- [ ] Release notes 写明系统要求（Windows 10/11 x64）
