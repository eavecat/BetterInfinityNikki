# BetterInfinityNikki 构建指南

## 前置条件

### 必需
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isdl.php)（约 10MB）

### 可选
- Visual Studio Build Tools 2022（如需 MicaSetup 支持）

## 安装 Inno Setup

通过 winget 安装：
```powershell
winget install JRSoftware.InnoSetup
```

安装完成后，如需中文安装界面，从 [Inno Setup Translations](https://jrsoftware.org/files/istrans/) 下载 `ChineseSimplified.isl` 放到 Inno Setup 的 `Languages` 目录下。

## 本地构建

运行一键构建脚本：
```cmd
BuildNikki\setup_build_nikki.cmd
```

可传入自定义版本号：
```cmd
BuildNikki\setup_build_nikki.cmd 0.2.0
```

不传参数则使用 csproj 中定义的版本号。

## 构建流程

1. **dotnet publish** — 编译并发布应用到 `BetterInfinityNikki\bin\x64\Release\net8.0-windows10.0.22621.0\publish\win-x64\`
2. **清理** — 删除 `.lib`、`ffmpeg*.dll`、`.pdb` 等无用文件
3. **ISCC 编译** — 使用 Inno Setup 将发布目录打包为安装程序

## 产出物

| 文件 | 说明 |
|------|------|
| `BetterIN_Setup_{版本}.exe` | 64 位离线安装包 |

安装包默认输出到 `BuildNikki\` 目录。

## 文件说明

```
BuildNikki/
├── betterin.iss            # Inno Setup 安装脚本配置
├── setup_build_nikki.cmd   # 一键构建脚本
└── README.md               # 本文档
```

## 安装包配置

`betterin.iss` 中的关键配置：

- **安装目录**：`{autopf}\BetterIN`（64 位系统为 `C:\Program Files\BetterIN`）
- **仅支持 64 位**：`ArchitecturesAllowed=x64compatible`
- **压缩方式**：LZMA2 Ultra
- **创建桌面快捷方式**：可选（安装时勾选）
- **创建开始菜单**：默认创建

## 常见问题

### 提示"未找到 Inno Setup"
确认 ISCC.exe 位于以下路径之一：
- `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`
- `D:\Program Files (x86)\Inno Setup 6\ISCC.exe`
- `C:\Program Files\Inno Setup 6\ISCC.exe`
- `D:\Program Files\Inno Setup 6\ISCC.exe`

如安装在其他位置，修改 `setup_build_nikki.cmd` 中的搜索路径。

### 提示"Couldn't open include file ChineseSimplified.isl"
下载中文语言文件放到 Inno Setup 的 `Languages` 目录，或删除 `betterin.iss` 中 `chinesesimplified` 语言行仅保留英文。
