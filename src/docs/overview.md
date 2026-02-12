# 项目概述

**STranslate** 是一个基于 Windows WPF 的翻译和 OCR 工具，采用插件化架构。它通过可扩展的插件支持多种翻译服务、OCR 提供商、TTS（文本转语音）和词汇管理。

## 主要功能

- **翻译服务**: 支持多种翻译引擎（内置 + 插件扩展）
- **OCR 文字识别**: 截图识别、静默识别
- **剪贴板监听**: 监听剪贴板变化并自动翻译（可通过全局热键 `Alt + Shift + A` 或主窗口按钮切换）
- **划词翻译**: 鼠标选中文本后通过热键翻译
- **截图翻译**: 截取屏幕区域进行 OCR 和翻译
- **TTS 朗读**: 文本转语音
- **生词本**: 保存和复习翻译过的单词
- **插件市场**: 内置插件市场，支持搜索、下载、升级社区插件

## 项目结构

```
├── STranslate/                    # 主 WPF 应用程序
│   ├── Core/                     # 核心服务（PluginManager, ServiceManager 等）
│   ├── Services/                 # 应用程序服务（TranslateService, OcrService 等）
│   ├── ViewModels/               # MVVM ViewModels
│   ├── Views/                    # WPF Views/Pages
│   ├── Controls/                 # 自定义 WPF 控件
│   ├── Converters/               # 值转换器
│   └── Plugin/                   # 插件接口定义（共享）
├── STranslate.Plugin/            # 共享插件接口和模型
└── Plugins/                      # 插件实现
    ├── STranslate.Plugin.Translate.*      # 官方内置翻译插件
    ├── STranslate.Plugin.Ocr.*            # 官方内置 OCR 插件
    ├── STranslate.Plugin.Tts.*            # 官方内置 TTS 插件
    ├── STranslate.Plugin.Vocabulary.*     # 官方内置词汇插件
    └── ThirdPlugins/                      # 社区/第三方插件
        ├── STranslate.Plugin.Translate.DeepLX/      # DeepLX 翻译插件
        ├── STranslate.Plugin.Translate.Gemini/      # Gemini 翻译插件
        ├── STranslate.Plugin.Translate.Ali/         # 阿里云翻译插件
        ├── STranslate.Plugin.Translate.QwenMt/      # 通义千问翻译插件
        ├── STranslate.Plugin.Translate.GoogleWebsite/ # Google 网页翻译插件
        ├── STranslate.Plugin.Translate.BingDict/    # 必应词典插件
        ├── STranslate.Plugin.Ocr.Gemini/            # Gemini OCR 插件
        ├── STranslate.Plugin.Ocr.Paddle/            # Paddle OCR 插件
        └── STranslate.Plugin.Vocabulary.Maimemo/    # 默默记单词生词本插件
```

## 构建命令

```powershell
# 构建 Debug 配置
dotnet build STranslate.slnx --configuration Debug

# 构建 Release 配置
dotnet build STranslate.slnx --configuration Release

# 构建特定版本（build.ps1 使用）
dotnet build STranslate.slnx --configuration Release /p:Version=2.0.0

# 运行构建脚本（清理、更新版本、构建、清理）
./build.ps1 -Version "2.0.0"
```

## 运行应用程序

```powershell
# 运行 Debug 构建
dotnet run --project STranslate/STranslate.csproj

# 或构建后直接运行可执行文件
./.artifacts/Debug/STranslate.exe
```

## 社区插件 (ThirdPlugins)

`ThirdPlugins` 目录用于存放**社区贡献的第三方插件**，这些插件通常：
- 由社区开发者独立维护
- 每个插件是一个**独立的 Git 仓库**（子模块或独立项目）
- 具有独立的版本号和发布周期
- 可以独立开发、测试和发布

### 推荐的插件目录结构（每个插件独立仓库）

```
STranslate.Plugin.Translate.DeepLX/
├── .git/                          # 独立 Git 仓库
├── README.md                      # 插件说明文档
├── CHANGELOG.md                   # 更新日志
├── LICENSE                        # 开源许可证
├── STranslate.Plugin.Translate.DeepLX/
│   ├── STranslate.Plugin.Translate.DeepLX.csproj  # 项目文件
│   ├── Main.cs                      # 插件主类（实现 TranslatePluginBase）
│   ├── Settings.cs                  # 配置模型类
│   ├── plugin.json                  # 插件元数据
│   ├── icon.png                     # 插件图标
│   ├── Languages/                   # 多语言文件
│   │   ├── zh-cn.xaml
│   │   ├── en.xaml
│   │   └── ...
│   ├── View/                        # 设置界面 XAML
│   │   ├── SettingsView.xaml
│   │   └── SettingsView.xaml.cs
│   └── ViewModel/                   # 设置界面 ViewModel
│       └── SettingsViewModel.cs
├── .artifacts/                     # 编译输出（可选）
└── obj/                            # 编译中间文件（gitignore）
```
