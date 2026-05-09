# 资源文件准备指南

## 📌 必需的资源文件

在运行 BetterIN 之前，你需要准备以下资源文件：

### 1. 应用图标 (必需)

**文件**: `logo.ico`  
**位置**: `Resources/Images/logo.ico`  
**要求**: 
- 格式: ICO
- 尺寸: 至少 256x256 像素（推荐包含多种尺寸：16x16, 32x32, 48x48, 256x256）
- 背景: 透明或纯色

**快速创建方法**:
1. 在线工具: https://www.icoconverter.com/
2. 使用 Photoshop/GIMP 导出为 .ico 格式
3. 从其他项目复制

### 2. Logo 图片 (必需)

**文件**: `logo.png`  
**位置**: `Resources/Images/logo.png`  
**要求**:
- 格式: PNG（支持透明背景）
- 尺寸: 建议 128x128 或 256x256 像素
- 用途: 标题栏图标、关于页面等

### 3. 字体文件 (强烈推荐)

**文件**: `MiSans-Regular.ttf`  
**位置**: `Resources/Fonts/MiSans-Regular.ttf`  
**说明**: 
- MiSans 是小米开源字体，美观且免费
- 下载: https://github.com/xiaomi/mi-sans-fonts
- 替代方案: 可以使用其他中文字体如 "Microsoft YaHei"（系统自带）

---

## 🚀 快速获取资源的方法

### 方法一：从 BetterGI 复制（推荐）

如果你已经有 BetterGenshinImpact 项目：

```bash
# 在项目根目录执行
cd D:\code\better-game-assistant

# 复制图标
copy BetterGenshinImpact\Resources\Images\logo.ico BetterInfinityNikki\Resources\Images\logo.ico

# 复制 Logo
copy BetterGenshinImpact\Resources\Images\logo.png BetterInfinityNikki\Resources\Images\logo.png

# 复制字体
copy BetterGenshinImpact\Resources\Fonts\MiSans-Regular.ttf BetterInfinityNikki\Resources\Fonts\MiSans-Regular.ttf
```

### 方法二：使用临时占位文件

如果暂时没有合适的资源，可以创建简单的占位文件：

#### 创建临时 ICO 文件
1. 使用画图工具创建一个 256x256 的 PNG 图片
2. 保存为 `logo.png`
3. 使用在线工具转换为 `.ico` 格式

#### 使用系统字体
修改 `App.xaml` 中的字体引用：

```xml
<!-- 原配置 -->
<FontFamily x:Key="TextThemeFontFamily">/Resources/Fonts/MiSans-Regular.ttf#MiSans</FontFamily>

<!-- 临时改为系统字体 -->
<FontFamily x:Key="TextThemeFontFamily">Microsoft YaHei</FontFamily>
```

### 方法三：下载开源资源

#### 图标资源
- [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons)
- [Material Design Icons](https://materialdesignicons.com/)

#### 字体资源
- [MiSans (小米字体)](https://github.com/xiaomi/mi-sans-fonts)
- [HarmonyOS Sans (华为字体)](https://developer.harmonyos.com/cn/design/resource-guidelines/font-0000001191704981)
- [Source Han Sans (思源黑体)](https://github.com/adobe-fonts/source-han-sans)

---

## ✅ 验证资源文件

放置好资源文件后，确认目录结构如下：

```
BetterInfinityNikki/
└── Resources/
    ├── Images/
    │   ├── logo.ico      ✅ 必须存在
    │   └── logo.png      ✅ 必须存在
    └── Fonts/
        └── MiSans-Regular.ttf  ✅ 强烈建议存在
```

---

## 🎨 设计建议

### 图标设计风格
- **简洁**: 避免过多细节
- **识别性**: 能够代表"无限暖暖"的主题
- **配色**: 建议使用粉色、紫色等温暖色调
- **风格**: 与 Fluent Design 保持一致

### Logo 设计元素
可以考虑包含：
- 无限符号 (∞)
- 暖暖的元素（蝴蝶结、星星等）
- 游戏相关的图标

### 颜色方案
推荐的配色：
- 主色: #FF69B4 (Hot Pink)
- 辅色: #9370DB (Medium Purple)
- 强调色: #FFD700 (Gold)

---

## 🔧 如果不想准备资源文件

你可以暂时修改项目配置跳过资源文件：

### 1. 修改 .csproj 文件

注释掉资源文件引用：

```xml
<!--<ItemGroup>
    <Resource Include="Resources\Images\*.jpg" />
    <Resource Include="Resources\Images\*.png" />
    <Resource Include="Resources\Images\*.ico" />
    <Resource Include="Resources\Fonts\*.ttf" />
</ItemGroup>-->
```

### 2. 修改 App.xaml

使用系统默认字体：

```xml
<FontFamily x:Key="TextThemeFontFamily">Microsoft YaHei</FontFamily>
```

### 3. 修改 MainWindow.xaml

移除图标引用或使用默认图标：

```xml
<ui:TitleBar.Icon>
    <!-- 暂时注释掉 -->
    <!--<ui:ImageIcon Source="pack://application:,,,/Resources/Images/logo.png" />-->
</ui:TitleBar.Icon>
```

**注意**: 这种方式只是临时方案，正式发布前还是需要准备合适的资源文件。

---

## 📞 需要帮助？

如果在准备资源文件时遇到问题：
1. 参考 `QUICKSTART.md` 文档
2. 查看 BetterGI 项目的资源文件作为示例
3. 使用在线图标制作工具快速生成

---

准备好资源文件后，就可以运行项目了！🎉
