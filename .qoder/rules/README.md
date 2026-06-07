# Lingma 项目规则说明

本目录包含 BetterGI 项目的 Lingma AI 助手规则配置，用于帮助 AI 更好地理解项目规范和编码风格。

## 规则文件列表

### 1. always_on.md - 始终生效规则
**类型**: Always（始终生效）  
**适用范围**: 所有智能会话和行间会话  
**文件大小**: ~10KB

包含项目的核心开发规范：
- MVVM 架构规则
- 依赖注入规范
- 图像识别基础
- 输入模拟规范
- 日志记录规范
- 异步编程规范
- 编译和构建要求

### 2. viewmodel_view.md - ViewModel 和 View 规范
**类型**: Specific Files（指定文件生效）  
**文件匹配**: `*.cs` (ViewModel 和 View 相关)  
**文件大小**: ~3.7KB

针对 ViewModel 和 View 的编码规范：
- ViewModel 编写规范
- View 代码后置规范
- XAML 绑定规范
- Behaviors 使用

### 3. service_config.md - 服务和配置规范
**类型**: Specific Files（指定文件生效）  
**文件匹配**: `*.cs` (Service 和 Config 相关)  
**文件大小**: ~4.6KB

针对服务类和配置类的规范：
- 服务类规范
- 配置类规范
- 异常处理规范
- 资源管理规范
- 日志记录规范

### 4. xaml_basics.md - XAML 基础规范
**类型**: Specific Files（指定文件生效）  
**文件匹配**: `*.xaml`  
**文件大小**: ~5KB

针对 XAML 文件的界面开发规范：
- WPF-UI 控件使用
- 布局规范
- 数据绑定规范
- 列表和集合
- 性能优化

### 5. image_recognition_basics.md - 图像识别基础规范
**类型**: Model Decision（模型决策）  
**触发场景**: 开发图像识别、自动化任务相关功能时  
**文件大小**: ~5.3KB

专门针对图像识别和自动化任务的开发规范：
- RecognitionObject 定义
- Assets 资产管理
- 捕获区域操作
- 输入模拟
- 任务触发器开发
- 独立任务开发
- 性能优化建议

### 6. directory_structure.md - 代码目录结构规范
**类型**: Model Decision（模型决策）  
**触发场景**: 创建新文件、新模块或重构代码时  
**文件大小**: ~8KB

详细的代码目录组织规范：
- 项目整体结构说明
- Core 核心模块规范
- GameTask 任务模块规范
- View/ViewModel 组织规范
- Service 服务层规范
- 新模块创建指南
- 目录命名约定

---

## 如何在 Lingma 中使用

### VS Code

1. 在智能会话中，规则会自动应用
2. 使用 `#rule` 可以手动引入特定规则
3. 规则会根据文件类型自动匹配

### JetBrains IDE

1. 在智能会话中，规则会自动应用
2. 使用 `@rule` 可以手动引入特定规则
3. 规则会根据文件类型自动匹配

### Visual Studio

1. 规则会在智能会话中自动生效
2. 根据文件扩展名自动应用相应规则

---

## 规则优先级

当规则和记忆存在冲突时，将**优先遵循规则执行**。

---

## 自定义规则

如果需要添加新的规则，请遵循以下命名规范：

- `always_on.md` - 始终生效的规则
- `{feature}_coding.md` - 针对特定功能的编码规范
- `{filetype}.md` - 针对特定文件类型的规范

规则文件应使用自然语言描述，单个文件不超过 10000 字符。

---

## 注意事项

1. **规则同步**: 规则文件存储在项目中，可以通过 Git 共享给团队成员
2. **个人规则**: 如果希望规则仅对个人生效，可以将 `.lingma/rules` 添加到 `.gitignore`
3. **规则更新**: 当项目规范发生变化时，请及时更新对应的规则文件
4. **字符限制**: 单个规则文件最大 10000 字符，超过部分会被自动截断

---

## 相关文档

- [Lingma 官方文档](https://help.aliyun.com/zh/lingma/rules)
- [BetterGI 项目文档](https://www.bettergi.com/doc.html)
- [AGENTS.md](../AGENTS.md) - AI 助手通用指令

---

## 更新日志

- **2026-05-22**: 最终版本，优化规则文件
  - always_on.md (10KB)
  - viewmodel_view.md (3.7KB)
  - service_config.md (4.6KB)
  - xaml_basics.md (5KB)
  - image_recognition_basics.md (5.3KB)
  - directory_structure.md (8KB) ⭐ 新增
  
  所有文件均符合 Lingma 10000 字符限制要求。
