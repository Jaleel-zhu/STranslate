# 参考信息

## 重要文件

### 核心文件

| 文件 | 用途 |
|------|------|
| `STranslate/App.xaml.cs` | 应用程序入口、DI 设置、生命周期 |
| `STranslate/Core/PluginManager.cs` | 插件发现、加载、安装 |
| `STranslate/Core/ServiceManager.cs` | 服务创建、生命周期 |
| `STranslate/Services/BaseService.cs` | 所有服务类型的基础 |

### 插件相关

| 文件 | 用途 |
|------|------|
| `STranslate.Plugin/IPlugin.cs` | 核心插件接口 |
| `STranslate.Plugin/PluginMetaData.cs` | 插件元数据模型 |
| `STranslate.Plugin/Service.cs` | 运行时服务实例 |
| `STranslate/Views/Pages/PluginMarketPage.xaml` | 插件市场页面 UI |
| `STranslate/ViewModels/Pages/PluginMarketViewModel.cs` | 插件市场视图模型 |
| `STranslate/Models/PluginMarketInfo.cs` | 市场插件数据模型 |
| `STranslate/Converters/PluginMarketConverters.cs` | 插件市场转换器 |

### 热键相关

| 文件 | 用途 |
|------|------|
| `STranslate/Core/HotkeySettings.cs` | 热键配置模型、热键注册管理 |
| `STranslate/Core/HotkeyModel.cs` | 热键数据结构、解析与验证 |
| `STranslate/Helpers/HotkeyMapper.cs` | 热键注册、低级别键盘钩子 |
| `STranslate/Controls/HotkeyControl.cs` | 热键设置自定义控件 |
| `STranslate/Controls/HotkeyDisplay.cs` | 热键显示自定义控件 |
| `STranslate/Views/Pages/HotkeyPage.xaml` | 热键设置页面 |

### 功能相关

| 文件 | 用途 |
|------|------|
| `STranslate/Helpers/ClipboardMonitor.cs` | 剪贴板监听实现（Win32 API） |
| `STranslate/Controls/ListBoxSelectedItemsBehavior.cs` | ListBox 多选行为附加属性 |
| `STranslate/Controls/HeaderControl.xaml` | 主窗口标题栏控件模板 |
| `STranslate/Controls/OutputControl.xaml` | 输出区域控件模板（含 TTS 按钮） |
| `STranslate/Controls/InputControl.xaml` | 输入区域控件模板（含 TTS 按钮） |

### 构建与配置

| 文件 | 用途 |
|------|------|
| `build.ps1` | Release 构建脚本 |
| `Directory.Packages.props` | 集中式 NuGet 版本 |

## 关键依赖

### 框架与运行时

| 依赖 | 用途 |
|------|------|
| **.NET 10.0-windows** | WPF 应用程序框架 |

### MVVM 与架构

| 依赖 | 用途 |
|------|------|
| **CommunityToolkit.Mvvm** | MVVM 模式支持（源生成器） |
| **Microsoft.Extensions.*** | 依赖注入、配置、日志等 |

### UI 组件

| 依赖 | 用途 |
|------|------|
| **iNKORE.UI.WPF.Modern** | 现代 UI 控件和主题 |

### 日志

| 依赖 | 用途 |
|------|------|
| **Serilog** | 结构化日志记录 |

### 热键与输入

| 依赖 | 用途 |
|------|------|
| **NHotkey.Wpf** | 全局热键注册 |
| **MouseKeyHook** | 鼠标键盘钩子（Ctrl+CC 等功能） |
| **ChefKeys** | Win 键热键支持 |

### 网络

| 依赖 | 用途 |
|------|------|
| **System.Net.Http** | HTTP 请求（支持代理） |

### 存储

| 依赖 | 用途 |
|------|------|
| **Microsoft.Data.Sqlite** | SQLite 数据库（历史记录） |

### 更新

| 依赖 | 用途 |
|------|------|
| **Velopack** | 自动更新框架 |

### 插件加载

| 依赖 | 用途 |
|------|------|
| **System.Reflection.MetadataLoadContext** | 插件程序集反射加载 |

### IL 织入

| 依赖 | 用途 |
|------|------|
| **Costura.Fody** | 程序集合并 |
| **MethodBoundaryAspect.Fody** | AOP 面向切面编程 |

### Win32 API

| 依赖 | 用途 |
|------|------|
| **Microsoft.Windows.CsWin32** | 类型安全的 P/Invoke |

## 修改指南

### 修改核心服务

| 服务 | 文件路径 |
|------|----------|
| TranslateService | `STranslate/Services/TranslateService.cs` |
| OcrService | `STranslate/Services/OcrService.cs` |
| TtsService | `STranslate/Services/TtsService.cs` |
| VocabularyService | `STranslate/Services/VocabularyService.cs` |

### UI 更改

- **Views**: `STranslate/Views/`
- **ViewModels**: `STranslate/ViewModels/`
- **框架**: 使用 CommunityToolkit.Mvvm 进行 MVVM
- **UI 组件**: 使用 iNKORE.UI.WPF.Modern 用于现代 UI 组件
