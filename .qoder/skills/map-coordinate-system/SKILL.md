---
name: map-coordinate-system
description: SIFT 地图坐标系统：tile 尺寸计算、image dimensions 推导、originX/Y 计算、坐标转换公式、map_config.json 配置规范。处理坐标偏移和比例错误的诊断。
license: MIT
metadata:
  author: project
  version: "1.0"
---

# SIFT 地图坐标系统

BetterInfinityNikki 和 BetterGenshinImpact 共享的 SIFT 特征地图坐标系统规范。

---

## 核心概念

### 坐标系层次

```
游戏坐标 (Game Coordinates)
    ↕  originX/Y + scale
特征图像坐标 (Feature Image Coordinates)
    ↕  tileSize × grid
瓦片坐标 (Tile Coordinates: Level-col-row)
```

### 关键公式

**图像尺寸计算**（从瓦片范围推导）：
```
imageWidth = (endX - startX + 1) × tileSize
imageHeight = (endY - startY + 1) × tileSize
```
其中 tileSize 通常为 256px。

**Origin 计算**（游戏 (0,0) 在图像中的像素位置）：
```
originX = (0 - minX) / (maxX - minX) × imageWidth
originY = (0 - minY) / (maxY - minY) × imageHeight
```
其中 minX/minY/maxX/maxY 是游戏坐标边界。

**坐标转换**：
```
游戏坐标 = (origin - 图像坐标) / scale
图像坐标 = origin - 游戏坐标 × scale
```
其中 `scale = MapImageBlockWidth / 1024f`。

---

## map_config.json 规范

每个地图在 `Assets/Map/{mapKey}/map_config.json` 中存储：

```json
{
  "worldId": 1,
  "imageWidth": 16384,
  "imageHeight": 16384,
  "gameBounds": {
    "minX": 0,
    "minY": -200000,
    "maxX": 1058400,
    "maxY": 1258400
  },
  "originX": 8192,
  "originY": 8192,
  "mapImageBlockWidth": 2048
}
```

### 字段说明

| 字段 | 来源 | 说明 |
|------|------|------|
| `worldId` | API | 用于与 `world/config/list` API 匹配 |
| `imageWidth` | tile 计算 | **不是**来自 API，是 tile 数量 × tileSize |
| `imageHeight` | tile 计算 | 同上 |
| `gameBounds` | API `max_bounds` | 游戏坐标边界（注意：API 返回的是 double-encoded JSON） |
| `originX` | 计算 | `(0 - minX) / (maxX - minX) × imageWidth` |
| `originY` | 计算 | `(0 - minY) / (maxY - minY) × imageHeight` |
| `mapImageBlockWidth` | 特征文件 | SIFT 特征块宽度（如 2048 对应 1024px 特征块） |

---

## 已知地图配置

### NikkiWorld (Nikki 世界)
- Tiles: X[0-63] Y[0-63] = 64×64 × 256 = **16384×16384**
- Game bounds: (0, -200000) → (1058400, 1258400)
- Origin: (8192, 8192)
- `mapImageBlockWidth`: 2048

### WuYouDao (无忧岛)
- Tiles: X[16-50] Y[5-38] = 35×34 × 256 = **8960×8704**
- Game bounds: (-30000, -50000) → (123296, 200305)
- Origin: ≈ (1754, 1739)
- API worldId: 10000002

### WoNiuCheng (蜗牛城)
- Tiles: 64×41 (非正方形)
- API worldId: 1010202

### WanXiangJing (万象境)
- Tiles: 64×64
- API worldId: 4020034

### HuaYanQunDao (花颜群岛)
- Tiles: 64×64
- API worldId: 10000001

### DanQingYu (丹青域)
- API worldId: 10000010

### DanQingZhiJing (丹青之境)
- API worldId: 10000027

---

## 常见 Bug 模式

### Bug 1: 坐标偏移到左上角 + 比例过小

**症状**：特征点出现在地图左上角，且整体偏小

**原因 A：`_mapImageBlockWidthScale` 是 readonly**
- `SceneBaseMap._mapImageBlockWidthScale` 是 `private readonly float`
- 构造时设置为 `mapImageBlockWidth / 1024f`
- `NikkiWorldMap.UpdateConfig()` 改变 `MapImageBlockWidth` 但 scale 不更新
- **修复**：改为计算属性 `=> MapImageBlockWidth / 1024f`

**原因 B：map_config.json 图像尺寸错误**
- 使用了 API 的 `map_size`（游戏坐标宽度）而非 tile 计算的像素尺寸
- **修复**：重新计算 `imageWidth = (endX - startX + 1) × 256`

**原因 C：Origin 计算错误**
- 对于有负游戏边界的地图（如 WuYouDao），origin 不能为 (0,0)
- **修复**：使用公式 `originX = (0 - minX) / (maxX - minX) × imageWidth`

### Bug 2: 切换地图后坐标转换失效

**症状**：从 NikkiWorld 切换到 WuYouDao 后，坐标转换结果错误

**原因**：`_mapImageBlockWidthScale` 只在构造时设置一次

**修复**：
```csharp
// Before (broken)
private readonly float _mapImageBlockWidthScale;

// After (fixed)
private float _mapImageBlockWidthScale => MapImageBlockWidth / 1024f;
```

---

## 代码位置参考

### BetterInfinityNikki
- `GameTask/Common/Map/Maps/Base/SceneBaseMap.cs` — 坐标转换核心
- `GameTask/Common/Map/Maps/NikkiWorldMap.cs` — UpdateConfig()
- `GameTask/Common/Map/Layers/BigMapNikkiLayer.cs` — SwitchMap(), LoadFeatures()
- `Assets/Map/*/map_config.json` — 每个地图的配置

### BetterGenshinImpact
- 同样的 SceneBaseMap 架构
- 6 个地图类型：Teyvat, TheChasm, Enkanomiya, SeaOfBygoneEras, AncientSacredMountain, TempleOfSpace

---

## 诊断流程

当遇到坐标偏移问题时：

1. **检查 map_config.json**
   - imageWidth/Height 是否来自 tile 计算（不是 API 的 map_size）？
   - gameBounds 是否正确（注意负值）？
   - originX/Y 是否使用公式计算（不是 0,0）？

2. **检查 _mapImageBlockWidthScale**
   - 是否是计算属性（不是 readonly 字段）？
   - 切换地图后值是否更新？

3. **检查特征文件**
   - .mat.png 是否正确保存为灰度？
   - 加载时是否 ConvertTo(CV_32FC1)？
   - 特征块大小是否与 mapImageBlockWidth 匹配？

---

**相关文件**：
- `AGENTS.md` — 项目规范
- MEMORY.md — 已知问题和架构决策
