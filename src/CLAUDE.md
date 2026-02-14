# CLAUDE.md

本文档为 Claude Code (claude.ai/code) 在处理本仓库代码时提供指导。

## 语言规则
- 所有解释、推理和评论必须用简体中文书写。
- 除非是代码、标识符或不可避免的技术术语，否则不得使用英语。
- 错误解释和总结必须用中文。

## 快速构建与调试

```powershell
# 构建并运行（最常用）
dotnet run --project STranslate/STranslate.csproj

# 或构建后运行
dotnet build STranslate.slnx --configuration Debug
./.artifacts/Debug/STranslate.exe
```

更多构建选项参见 [项目概述](docs/overview.md)。

## 多语言支持

本项目支持以下五种语言，语言文件位于 [`STranslate/Languages/`](STranslate/Languages/) 目录：

| 语言 | 文件 | 说明 |
|------|------|------|
| 简体中文 | `zh-cn.xaml` | 默认语言 |
| 繁体中文 | `zh-tw.xaml` | 台湾/香港地区 |
| English | `en.xaml` | 英语 |
| 日本語 | `ja.xaml` | 日语 |
| 한국어 | `ko.xaml` | 韩语 |

### 添加或修改国际化字符串

1. 在所有语言文件中添加相同的键值（参考现有字符串格式）
2. 使用 `_i18n.GetTranslation("KeyName")` 在代码中获取翻译
3. 使用 `Internationalization.GetString("KeyName")` 在 XAML 中绑定

### 开发注意事项

- 新增用户可见的提示信息时，必须添加对应的国际化字符串
- 建议按模块分类组织语言文件（参考现有文件的注释分组）
- 所有语言文件必须保持键的一致性（相同的键存在于所有语言文件中）

## 文档导航

本文档已按功能模块拆分为以下子文档：

### 快速开始
- [**项目概述**](docs/overview.md) - STranslate 项目简介、主要功能、构建命令

### 架构设计
- [**架构设计**](docs/architecture.md) - 核心架构说明
  - 启动流程 - 应用程序启动过程
  - 插件系统 - 插件加载与管理
  - 服务管理 - Service 与 Plugin 的关系
  - 关键接口 - IPlugin、IPluginContext 等接口定义
  - 数据流 - 翻译功能的数据流示例

### 功能特性
- [**功能特性**](docs/features.md) - 热键系统、剪贴板监听、历史记录

### 存储与配置
- [**存储与配置**](docs/storage.md) - 设置架构、存储位置

### 插件开发
- [**插件开发指南**](docs/plugin.md) - 插件开发、包格式、社区插件开发

### 开发参考
- [**参考信息**](docs/reference.md) - 关键文件索引、修改核心服务/UI、技术栈与依赖项

## 快速参考

### 按任务类型查找

| 任务类型 | 关键文件/位置 | 详细文档 |
|---------|-------------|---------|
| **UI 页面开发** | `STranslate/Views/Pages/*.xaml` | [参考信息](docs/reference.md) |
| **ViewModel 逻辑** | `STranslate/ViewModels/Pages/*.cs` | [架构设计](docs/architecture.md) |
| **添加新服务** | `STranslate/ViewModels/Preference/Services/*.cs` | [架构设计](docs/architecture.md#服务管理) |
| **开发插件** | `Plugins/` 目录 | [插件开发指南](docs/plugin.md) |
| **修改翻译服务** | `STranslate/ViewModels/Preference/Services/TranslateServiceViewModel.cs` | - |
| **修改 OCR 服务** | `STranslate/ViewModels/Preference/Services/OCRServiceViewModel.cs` | - |
| **修改 TTS 服务** | `STranslate/ViewModels/Preference/Services/TTSServiceViewModel.cs` | - |
| **热键功能** | `STranslate/Core/HotkeySettings.cs` | [功能特性](docs/features.md) |
| **剪贴板监听** | `STranslate/Helpers/ClipboardMonitor.cs` | [功能特性](docs/features.md) |
| **历史记录** | `STranslate/Core/SqlService.cs` | [功能特性](docs/features.md) |
| **添加国际化** | `STranslate/Languages/*.xaml` | [多语言支持](#多语言支持) |
| **存储配置** | `STranslate/Models/ConfigModel.cs` | [存储与配置](docs/storage.md) |

### 常用文件速查

```
主窗口              -> STranslate/Views/MainWindow.xaml
设置窗口            -> STranslate/Views/SettingsWindow.xaml
翻译输入控件        -> STranslate/Controls/InputControl.xaml
翻译输出控件        -> STranslate/Controls/OutputControl.xaml
头部控件            -> STranslate/Controls/HeaderControl.xaml

插件页面            -> STranslate/Views/Pages/PluginPage.xaml（已安装插件 + 插件市场）
常规设置页面        -> STranslate/Views/Pages/GeneralPage.xaml
热键设置页面        -> STranslate/Views/Pages/HotkeyPage.xaml

核心翻译服务        -> STranslate/Core/TranslationService.cs
HTTP 服务           -> STranslate/Core/HttpService.cs
设置管理            -> STranslate/Core/Settings.cs
```

### 常见修改场景

| 想实现的功能 | 参考实现位置 |
|------------|-------------|
| 添加新的设置项 | `STranslate/Models/ConfigModel.cs` + `STranslate/Views/Pages/GeneralPage.xaml` |
| 添加页面到设置窗口 | `STranslate/Views/SettingsWindow.xaml` 的导航菜单 |
| 添加新的图标按钮 | 参考 `PluginPage.xaml` 中的 `ActionIconStyle` |
| 添加带状态的按钮 | 参考 `PluginPage.xaml` 市场视图的多状态图标样式 |
| 添加进度显示 | 参考 `PluginPage.xaml` 市场视图的下载进度条 |
| 添加拖放功能 | 参考 `PluginPage.xaml` 的 `AllowDrop` 实现 |
| 视图切换（Visibility） | 参考 `PluginPage.xaml` 的 `IsMarketView` 绑定 |

## 最近更新 (2026-02-14)

### 插件页面合并
- **页面合并**：`PluginPage.xaml` 和 `PluginMarketPage.xaml` 合并为统一的插件管理页面
- **视图切换**：通过"市场"/"返回"按钮在同一页面内切换"已安装插件"和"插件市场"视图
- **延迟加载**：插件市场数据首次切换到该视图时才加载，避免页面初始化时的网络卡顿
- **ViewModel 合并**：`PluginMarketViewModel.cs` 功能合并到 `PluginViewModel.cs`

### 关键文件变更
- `STranslate/Views/Pages/PluginPage.xaml` - 整合市场视图内容，添加视图切换支持
- `STranslate/ViewModels/Pages/PluginViewModel.cs` - 合并市场功能（加载、下载、升级等）
- `STranslate/Views/SettingsWindow.xaml` - 移除独立的插件市场导航项
- `STranslate/Views/SettingsWindow.xaml.cs` - 移除 PluginMarketPage 导航逻辑
- ~~`STranslate/Views/Pages/PluginMarketPage.xaml`~~ - 已删除
- ~~`STranslate/Views/Pages/PluginMarketPage.xaml.cs`~~ - 已删除
- ~~`STranslate/ViewModels/Pages/PluginMarketViewModel.cs`~~ - 已删除

### 实现细节
- 使用 `IsMarketView` 属性控制视图切换（`BoolToVisibilityConverter`）
- 使用 `IsMarketInitialized` 标志实现延迟加载
- `ToggleMarketViewCommand` 命令处理视图切换
- 保留原有的拖放安装功能（仅在已安装视图可用）

