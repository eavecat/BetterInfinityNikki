---
name: kachina-build
description: kachina installer 打包工作流：生成 updater、gen metadata、pack installer。处理 PowerShell 5.1 编码陷阱、kachina-builder.exe 调用方式、CLI 参数语义。
license: MIT
metadata:
  author: project
  version: "1.0"
---

# kachina 打包工作流

构建 BetterInfinityNikki 的 kachina 安装包和自动更新程序。

---

## 关键约束（必须遵守）

1. **kachina-builder.exe 必须通过 `cmd /c` 调用** — 在 PowerShell 中直接使用 `&` 调用会抛出 `ApplicationFailedException`
2. **.ps1 文件必须保存为 UTF-8 with BOM** — PowerShell 5.1 在中文 Windows 上使用 GBK (code page 936) 读取 .ps1，无 BOM 的 UTF-8 文件中多字节字符会导致解析失败
3. **所有路径使用 `$PSScriptRoot`** — 确保脚本从任意目录执行都能正常工作
4. **`-t` 是自定义图片/CSS，`-m` 是 metadata.json** — 不要混淆这两个 flag

---

## 完整构建流程

### 前置条件
- `kachina-builder.exe` 在 `BuildNikki/` 目录
- 已运行 `upload_1_build_dist.ps1` 生成 `dist/BetterIN/` 发布产物
- `kachina_nikki.json` 配置文件存在

### 步骤 1：生成 Updater (BetterIN.update.exe)

```powershell
cmd /c "cd /d `"$distDir`" && `"$kachina`" pack -c `"$PSScriptRoot\kachina_nikki.json`" -o `"$distDir\BetterIN.update.exe`" --icon `"$icon`" -t `"$left_icon`" 2>nul"
```

**参数说明：**
- `-c`: kachina 配置文件路径
- `-o`: 输出文件路径
- `--icon`: exe 图标 (.ico)
- `-t`: 自定义左侧面板图片 (.webp 或 .css)
- `2>nul`: 抑制 stderr（PowerShell 5.1 中 stderr 会变成终止异常）

### 步骤 2：生成 Metadata + Hashed 文件

```powershell
cmd /c "cd /d `"$distDir`" && `"$kachina`" gen -j 6 -i . -m `"$PSScriptRoot\metadata.json`" -o `"$PSScriptRoot\hashed`" -t $version -r liuxia/betterin -u BetterIN.update.exe 2>nul"
```

**参数说明：**
- `-j 6`: 并行线程数
- `-i .`: 输入目录（当前目录）
- `-m`: metadata.json 输出路径
- `-o`: hashed 文件输出目录
- `-t`: 版本号
- `-r`: GitHub 仓库 (user/repo)
- `-u`: updater 文件名

### 步骤 3：生成 Installer

```powershell
cmd /c "cd /d `"$distDir`" && `"$kachina`" pack -c `"$PSScriptRoot\kachina_nikki.json`" -m `"$PSScriptRoot\metadata.json`" -d `"$PSScriptRoot\hashed`" -o `"$PSScriptRoot\BetterIN.Install.$version.exe`" --icon `"$icon`" -t `"$left_icon`" 2>nul"
```

**参数说明：**
- `-m`: metadata.json（step 2 生成的）
- `-d`: hashed 文件目录（step 2 生成的）
- 其余同 updater

---

## CLI 参数速查

| Flag | 含义 | 示例 |
|------|------|------|
| `-c` | 配置文件 | `-c kachina_nikki.json` |
| `-o` | 输出路径 | `-o BetterIN.Install.exe` |
| `-t` | 自定义图片/CSS（左侧面板） | `-t left.webp` |
| `-m` | metadata.json 路径 | `-m metadata.json` |
| `-d` | hashed 文件目录 | `-d hashed/` |
| `-i` | 输入目录 | `-i .` |
| `-j` | 并行线程数 | `-j 6` |
| `-r` | GitHub 仓库 | `-r liuxia/betterin` |
| `-u` | updater 文件名 | `-u BetterIN.update.exe` |
| `--icon` | exe 图标 | `--icon logo.ico` |

---

## kachina 二进制格式（调试/解析用）

安装包是一个 PE 文件，DOS header 区域被覆写：

```
Offset 78: !KachinaInstaller! (18 bytes)
+ 5 × uint32 BE: base_end, config_sz, theme_sz, index_sz, metadata_sz
```

**TLV 段顺序**：CONFIG → IMAGE → INDEX → META

每段格式：`!in\0` (4 bytes) + name_len (2 bytes BE) + name + size (4 bytes BE) + data

---

## kachina_nikki.json 配置

```json
{
  "appName": "BetterIN",
  "description": "更好的无限暖暖",
  "exeName": "BetterIN.exe",
  "publisher": "liuxia",
  "regName": "BetterIN",
  "runtimes": ["Microsoft.DotNet.DesktopRuntime.8"],
  "source": [],
  "title": "BetterIN",
  "windowTitle": "BetterIN 安装程序"
}
```

**`source` 字段**：
- `[]` 或 `""`: 禁用在线更新检查（本地测试用）
- `"direct+packed+http://..."`: 直连模式，服务器需支持 Range requests
- `"dfs2+packed+http://..."`: DFS2 协议，需专用服务器

---

## 常见错误与修复

| 错误 | 原因 | 修复 |
|------|------|------|
| `ApplicationFailedException` | PowerShell 直接 `&` 调用 kachina-builder.exe | 用 `cmd /c "..."` 包装 |
| `Cannot find path because it does not exist` | UTF-8 无 BOM 的 .ps1 在中文 Windows 上解析失败 | 保存为 UTF-8 with BOM |
| `-m logo.webp` 静默失败 | `-m` 是 metadata.json，不是图片 | 改用 `-t logo.webp` |
| Installer 启动后 WebView2 崩溃 | `source` 指向不存在的 DFS2 服务器 | 设置 `"source": ""` |
| PowerShell `[BitConverter]::ToUInt32` 失败 | 数组切片创建过小的数组 | 使用 `[System.IO.BinaryReader]` 或手动字节移位 |

---

## 好的实践

- 构建前先检查版本号：从 `.csproj` 读取
- 使用 `Start-Sleep -Milliseconds 500` 在 gen 和 pack 之间等待文件系统同步
- 构建后检查文件是否存在来判断成功/失败
- kachina `gen` 处理大文件（>60MB）可能需要 >2 分钟

---

**相关文件**：
- `BuildNikki/build_installer.ps1` — 主构建脚本
- `BuildNikki/kachina_nikki.json` — kachina 配置
- `BuildNikki/setup_build.ps1` — 离线 7z 打包
- `BuildNikki/upload_1_build_dist.ps1` — 编译发布
