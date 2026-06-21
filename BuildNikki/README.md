# BetterInfinityNikki 构建指南

## 前置条件

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PowerShell 5.1+（Windows 10/11 自带）
- [kachina-builder.exe](https://github.com/YuehaiTeam/kachina-installer/releases)（在线安装包构建）

## 本地构建

### 离线安装包（7z 压缩包）

```powershell
.\setup_build.ps1
```

可传入自定义版本号：
```powershell
.\setup_build.ps1 0.2.0
```

### CI 构建（无交互）

```powershell
.\setup_build_for_appveyor.ps1
```

## 发布流程

### 第1步：构建分发包

```powershell
.\upload_1_build_dist.ps1
```

### 第2步：生成更新器 + 安装器

```powershell
.\build_installer.ps1
```

### 第3步：生成哈希 + 压缩

```powershell
.\upload_2_zip_dist.ps1
```

## 文件说明

```
BuildNikki/
├── setup_build.ps1                 # 离线构建脚本（7z 打包）
├── setup_build_for_appveyor.ps1    # CI 构建脚本（无交互）
├── upload_1_build_dist.ps1         # 发布第1步：编译输出
├── build_installer.ps1             # 发布第2步：生成更新器 + 安装器
├── upload_2_zip_dist.ps1           # 发布第3步：哈希 + 压缩
├── kachina_nikki.json              # kachina 安装器配置
├── micasetup.json                  # MicaSetup 配置（未使用）
├── kachina-builder.exe             # kachina 打包工具
├── MicaSetup.Tools/ → Build/MicaSetup.Tools/  # 7-Zip 工具（目录联接）
└── README.md                       # 本文档
```

### 脚本对照 BetterGI

| BuildNikki | BetterGI | 说明 |
|------------|----------|------|
| `setup_build.ps1` | `setup_build.cmd` | 离线构建 |
| `setup_build_for_appveyor.ps1` | `setup_build_for_appveyor.cmd` | CI 构建 |
| `upload_1_build_dist.ps1` | `upload_1_build_dist.cmd` | 发布第1步 |
| `build_installer.ps1` | 无 | kachina 在线安装器生成 |
| `upload_2_zip_dist.ps1` | `upload_2_zip_dist.ps1` | 发布第2步 |

## kachina 安装器

### 打包流程

```
kachina_nikki.json ─┐
                    ├─ kachina-builder pack ─→ BetterIN.update.exe (更新器)
dist/BetterIN/ ────┤
                    ├─ kachina-builder gen  ─→ metadata.json + hashed/
                    └─ kachina-builder pack ─→ BetterIN.Install.{版本}.exe (安装器)
```

### 安装器二进制结构

```
BetterIN.Install.exe
┌─────────────────────────┐
│  kachina-installer 模板  │
├─────────────────────────┤ offset 78
│ !KachinaInstaller!      │ 魔术头
│ base_end (4B)           │ 模板大小
│ config_sz (4B)          │ CONFIG 段大小
│ theme_sz (4B)           │ IMAGE 段大小
│ index_sz (4B)           │ INDEX 段大小
│ metadata_sz (4B)        │ META 段大小
├─────────────────────────┤ offset base_end
│ \0CONFIG (TLV)          │ kachina_nikki.json
│ \0IMAGE  (TLV)          │ 左侧图片
│ \0INDEX  (TLV)          │ 文件偏移索引
│ \0META   (TLV)          │ metadata.json
├─────────────────────────┤
│ zstd 压缩的应用文件      │
└─────────────────────────┘
```

### source 类型

| URI 格式 | 协议 | 说明 |
|----------|------|------|
| `dfs2+packed+https://...` | DFS2 | 会话式分块下载 |
| `dfs+hashed+https://...` | DFS1 | 单文件哈希下载 |
| `https://xxx.exe` | Direct | 从 exe 内嵌资源读取 |
| `https://xxx.json` | Direct | 下载 metadata JSON |

## DFS2 测试服务器

位于 `nikki_js_assistant/src/dfs2_server.ts`，端口 5001：

```powershell
cd D:\code\nikki_js_assistant
npx tsx src/dfs2_server.ts
```

API 接口：
- `GET /resource/:name` — 返回资源元数据
- `POST /session/:resourceId` — 创建下载会话
- `GET /session/:sessionId/:range` — 获取分块 URL
- `GET /download/:resourceId/:range` — 按 Range 下载
