# 如何发布 NukeTree

只发布到 **GitHub**：仓库（源码）+ Release（绿色便携版 / 安装版 zip）。

> 本仓库已内置 CI：推 `v*` tag 会自动构建并创建 Release。
> 也可以在本地一键打包后手动上传。

---

## 0. 准备

- [Git for Windows](https://git-scm.com/download/win)
- GitHub 账号
- 配置 Git 身份（只需一次）：

```powershell
git config --global user.name "你的名字"
git config --global user.email "you@example.com"
```

---

## 1. 把源码推到 GitHub

仓库已建好时直接改代码 → 提交 → 推送：

```powershell
cd "D:\deepseek Harness\NukeTree"
git add .
git commit -m "feat: 描述这次改动"
git push
```

首次从零开始时（先在网页建空仓库 `NukeTree`，**不勾** README）：

```powershell
cd /d "D:\deepseek Harness\NukeTree"
git init -b main
git add .
git commit -m "feat: NukeTree 1.0.0"
git remote add origin https://github.com/<你的用户名>/NukeTree.git
git push -u origin main
```

---

## 2. 打 tag，自动生成 Release（推荐）

```powershell
git tag v1.0.1
git push origin v1.0.1
```

推送后 GitHub Actions（`.github/workflows/build-release.yml`）会：

- Windows runner 编译 `dist/NukeTree.exe`
- 生成 `NukeTree-<版本>-win64.zip` + `.sha256.txt`
- 创建 GitHub Release 并上传这两个文件

到仓库 **Actions** 页面确认运行成功，然后 **Releases** 里即可下载。

### 手动打包 + 上传（不想用 Actions 时）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\make-release.ps1 -Version 1.0.0
```

产物在 `release\`：

| 文件 | 说明 |
|---|---|
| `NukeTree-1.0.0-win64.zip` | 绿色便携版 |
| `NukeTree-1.0.0-win64-setup.zip` | 安装版（安装.cmd → LocalAppData + 开始菜单 + 卸载.cmd） |
| `NukeTree-1.0.0-win64.sha256.txt` 等 | 校验和 |

然后 GitHub → Releases → **Draft a new release** → 选 tag → 上传 zip 和 sha256 → Publish。

---

## 3. 版本升级流程

```powershell
# 1) 改代码、更新 README/RELEASE_NOTES
# 2) 更新 scripts/make-release.ps1 默认版本或传 -Version
# 3) 提交
git add .
git commit -m "chore: release v1.0.1"
git push

# 4) 打 tag 并推送（触发 CI 自动发 Release）
git tag v1.0.1
git push origin v1.0.1
```

---

## 4. 常见问题

| 问题 | 处理 |
|---|---|
| GitHub Actions 构建失败 | 看 Actions 日志；常见原因是 `.ps1` 编码（必须 UTF-8 **带 BOM**）或标签名不是 `v*` |
| push 提示无权限 | 用浏览器登录 GitHub 凭据，或使用 PAT（repo 权限）作为密码 |
| push 报 rejected（远程有新提交） | `git pull --rebase origin main` 后重新 push |
| 想撤 Release | Releases 页面删除该 Release，必要时 `git push origin :refs/tags/v1.0.0` 删 tag |
| 旧分支残留 | 仓库 Settings → Branches 里删掉，或 `git push origin :旧分支名` |

---

## 5. 发布前检查清单

- [ ] `build.ps1` 在本机构建成功，`dist\NukeTree.exe` 能双击运行
- [ ] `release\` 里的 zip 包能解压运行、安装版能装/卸
- [ ] `RELEASE_NOTES.md` 写了本次更新内容
- [ ] `.ps1` 文件均为 **UTF-8 with BOM**
- [ ] `README.md` 截图路径正确（`screenshots/...`）
- [ ] 不提交 `dist/`、`release/`、`settings.json`、`备份3D/`、`launch.log`
- [ ] tag 名与 `RELEASE_NOTES.md` 版本一致
