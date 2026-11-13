#!/usr/bin/env node
/**
 * nuketree — Windows 3D 应用彻底清理
 * npm CLI 启动器：npx nuketree / npm i -g nuketree && nuketree
 */
'use strict';

const path = require('path');
const fs = require('fs');
const { spawn } = require('child_process');

const args = process.argv.slice(2);

function printHelp() {
  const pkg = require(path.join(__dirname, '..', 'package.json'));
  console.log(`
NukeTree ${pkg.version} — 3D 应用彻底清理（Windows x64）

用法:
  nuketree [选项] [应用关键字...]

选项:
  -h, --help       显示帮助
  -v, --version    显示版本
  --exe <path>     指定 NukeTree.exe 路径（默认使用包内 dist/NukeTree.exe）
  --               之后的参数原样传给 NukeTree.exe

示例:
  nuketree
  nuketree --kw=clash
  npx nuketree

说明:
  - 需要 Windows 10/11 x64 与 .NET Framework 4.x（系统自带）
  - 删除系统目录/服务时请以管理员身份运行终端
  - 源码与绿色版: https://github.com/<your-account>/NukeTree
`);
}

if (args.includes('-h') || args.includes('--help')) {
  printHelp();
  process.exit(0);
}
if (args.includes('-v') || args.includes('--version')) {
  console.log(require(path.join(__dirname, '..', 'package.json')).version);
  process.exit(0);
}

let exe = path.join(__dirname, '..', 'dist', 'NukeTree.exe');
const exeArg = args.indexOf('--exe');
if (exeArg >= 0 && args[exeArg + 1]) {
  exe = path.resolve(args[exeArg + 1]);
  args.splice(exeArg, 2);
}

if (process.platform !== 'win32') {
  console.error('NukeTree 仅支持 Windows（当前平台: ' + process.platform + '）');
  process.exit(1);
}
if (!fs.existsSync(exe)) {
  console.error('找不到 ' + exe);
  console.error('请重新安装软件包，或在源码目录运行 build.ps1 生成 dist/NukeTree.exe');
  process.exit(1);
}

// 传递 --kw= 等参数给主程序（源码支持 --kw= / --apps / --downloads / --plugins / --settings）
const child = spawn(exe, args, {
  detached: true,
  stdio: 'ignore',
  windowsHide: false,
});
child.on('error', (err) => {
  console.error('启动 NukeTree 失败: ' + err.message);
  process.exit(1);
});
child.unref();
console.log('NukeTree 已启动' + (args.length ? '（参数: ' + args.join(' ') + '）' : ''));
