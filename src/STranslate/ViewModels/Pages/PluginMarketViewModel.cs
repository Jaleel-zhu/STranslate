using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Core;
using STranslate.Plugin;
using STranslate.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows.Data;

namespace STranslate.ViewModels.Pages;

public partial class PluginMarketViewModel : ObservableObject
{
    private readonly IHttpService _httpService;
    private readonly PluginService _pluginService;
    private readonly Internationalization _i18n;
    private readonly ISnackbar _snackbar;
    private readonly Settings _settings;
    public DataProvider DataProvider { get; }

    /// <summary>
    /// 插件列表数据源
    /// </summary>
    private readonly CollectionViewSource _pluginsCollectionView;

    /// <summary>
    /// 插件列表视图
    /// </summary>
    public ICollectionView PluginsView => _pluginsCollectionView.View;

    /// <summary>
    /// 插件列表
    /// </summary>
    public ObservableCollection<PluginMarketInfo> Plugins { get; } = [];

    /// <summary>
    /// 插件市场 JSON 数据源 URL
    /// </summary>
    private const string PluginsJsonUrl = "https://fastly.jsdelivr.net/gh/STranslate/STranslate-doc@main/vitepress/plugins.json";

    /// <summary>
    /// 是否需要重启（有升级操作时）
    /// </summary>
    private bool _needsRestart;

    public PluginMarketViewModel(
        IHttpService httpService,
        PluginService pluginService,
        Internationalization i18n,
        ISnackbar snackbar,
        Settings settings,
        DataProvider dataProvider)
    {
        _httpService = httpService;
        _pluginService = pluginService;
        _i18n = i18n;
        _snackbar = snackbar;
        _settings = settings;
        DataProvider = dataProvider;

        _pluginsCollectionView = new CollectionViewSource
        {
            Source = Plugins
        };
        _pluginsCollectionView.Filter += OnPluginsFilter;

        // 初始化时加载插件列表
        _ = LoadPluginsAsync();
    }

    #region 属性和命令

    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PluginType SelectedPluginType { get; set; } = PluginType.All;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? LoadingStatus { get; set; }

    partial void OnFilterTextChanged(string value) => _pluginsCollectionView.View?.Refresh();

    partial void OnSelectedPluginTypeChanged(PluginType value) => _pluginsCollectionView.View?.Refresh();

    #endregion

    #region 加载插件列表

    [RelayCommand]
    private async Task LoadPluginsAsync()
    {
        IsLoading = true;
        LoadingStatus = _i18n.GetTranslation("PluginMarketLoading");

        try
        {
            Plugins.Clear();

            // 1. 获取插件标识符列表
            var pluginIds = await _httpService.GetAsync<List<string>>(PluginsJsonUrl);

            if (pluginIds == null || pluginIds.Count == 0)
            {
                LoadingStatus = _i18n.GetTranslation("PluginMarketNoPlugins");
                return;
            }

            // 2. 并行获取每个插件的详细信息
            var tasks = pluginIds.Select(GetPluginInfoAsync);
            var plugins = await Task.WhenAll(tasks);
            //var firstId = pluginIds.First();
            //var plugins = await Task.WhenAll(GetPluginInfoAsync(firstId));

            // 3. 添加到集合
            foreach (var plugin in plugins.Where(p => p != null))
            {
                Plugins.Add(plugin!);
            }

            // 4. 更新安装状态
            UpdatePluginStatus();

            LoadingStatus = null;
        }
        catch (Exception ex)
        {
            LoadingStatus = _i18n.GetTranslation("PluginMarketLoadError");
            _snackbar.ShowError($"{_i18n.GetTranslation("PluginMarketLoadError")}: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 获取单个插件信息
    /// </summary>
    private async Task<PluginMarketInfo?> GetPluginInfoAsync(string pluginId)
    {
        try
        {
            // 解析插件ID格式: Author/STranslate.Plugin.Type.Name 或 STranslate.Plugin.Type.Name
            var parts = pluginId.Split('/');
            var packageName = parts.Length > 1 ? parts[1] : parts[0];
            var author = parts.Length > 1 ? parts[0] : "STranslate";

            // 解析包名: STranslate.Plugin.Type.Name
            var nameParts = packageName.Split('.');
            if (nameParts.Length < 4) return null;

            var type = nameParts[2]; // Translate, Ocr, Tts, Vocabulary

            // 尝试获取 plugin.json（先 main 分支，失败则回退到 master）
            var pluginInfo = await TryGetPluginInfoAsync(author, packageName);
            if (pluginInfo == null) return null;

            // 尝试获取多语言资源（zh-cn.json）
            var (name, description) = await TryGetLocalizedInfoAsync(author, packageName, pluginInfo.Name, pluginInfo.Description);

            // 构建下载 URL
            var downloadUrl = $"https://github.com/{author}/{packageName}/releases/download/v{pluginInfo.Version}/{packageName}.spkg";

            // 确定实际使用的分支（用于图标等资源）
            var branch = await GetWorkingBranchAsync(author, packageName);

            return new PluginMarketInfo
            {
                PluginId = pluginInfo.PluginID ?? string.Empty,
                Name = name ?? packageName,
                Author = pluginInfo.Author ?? author,
                Type = type,
                Version = pluginInfo.Version ?? "1.0.0",
                Description = description ?? string.Empty,
                Website = pluginInfo.Website ?? $"https://github.com/{author}/{packageName}",
                IconUrl = $"https://fastly.jsdelivr.net/gh/{author}/{packageName}@{branch}/{packageName}/icon.png",
                DownloadUrl = downloadUrl,
                PackageName = packageName
            };
        }
        catch
        {
            // 单个插件加载失败不影响其他插件
            return null;
        }
    }

    /// <summary>
    /// 尝试获取 plugin.json，main 分支失败则回退到 master 分支
    /// </summary>
    private async Task<PluginInfo?> TryGetPluginInfoAsync(string author, string packageName)
    {
        // 先尝试 main 分支
        var mainUrl = $"https://fastly.jsdelivr.net/gh/{author}/{packageName}@main/{packageName}/plugin.json";
        try
        {
            return await _httpService.GetAsync<PluginInfo>(mainUrl);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // 404 则回退到 master 分支
            var masterUrl = $"https://fastly.jsdelivr.net/gh/{author}/{packageName}@master/{packageName}/plugin.json";
            try
            {
                return await _httpService.GetAsync<PluginInfo>(masterUrl);
            }
            catch
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取可用的分支名称（main 或 master）
    /// </summary>
    private async Task<string> GetWorkingBranchAsync(string author, string packageName)
    {
        var mainUrl = $"https://fastly.jsdelivr.net/gh/{author}/{packageName}@main/{packageName}/plugin.json";
        try
        {
            await _httpService.GetAsync(mainUrl, CancellationToken.None);
            return "main";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return "master";
        }
        catch
        {
            return "main";
        }
    }

    /// <summary>
    /// 尝试获取多语言资源，失败则使用默认值
    /// </summary>
    private async Task<(string Name, string Description)> TryGetLocalizedInfoAsync(
        string author, string packageName, string defaultName, string defaultDescription)
    {
        var branch = await GetWorkingBranchAsync(author, packageName);
        var langUrl = $"https://fastly.jsdelivr.net/gh/{author}/{packageName}@{branch}/{packageName}/Languages/zh-cn.json";

        try
        {
            var langInfo = await _httpService.GetAsync<PluginLanguageInfo>(langUrl);
            if (langInfo != null)
            {
                return (
                    !string.IsNullOrEmpty(langInfo.Name) ? langInfo.Name : defaultName,
                    !string.IsNullOrEmpty(langInfo.Description) ? langInfo.Description : defaultDescription
                );
            }
        }
        catch
        {
            // 多语言资源获取失败，使用默认值
        }

        return (defaultName, defaultDescription);
    }

    /// <summary>
    /// 更新插件安装状态
    /// </summary>
    private void UpdatePluginStatus()
    {
        // 创建本地插件字典
        var localPlugins = _pluginService.PluginMetaDatas
            .ToDictionary(p => p.PluginID, p => p);

        foreach (var marketPlugin in Plugins)
        {
            if (localPlugins.TryGetValue(marketPlugin.PluginId, out var localPlugin))
            {
                marketPlugin.IsInstalled = true;
                marketPlugin.InstalledVersion = localPlugin.Version;

                // 比较版本
                var comparison = CompareVersions(localPlugin.Version, marketPlugin.Version);
                marketPlugin.CanUpgrade = comparison < 0;
            }
            else
            {
                marketPlugin.IsInstalled = false;
                marketPlugin.CanUpgrade = false;
                marketPlugin.InstalledVersion = null;
            }
        }
    }

    #endregion

    #region 过滤和搜索

    private void OnPluginsFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not PluginMarketInfo plugin)
        {
            e.Accepted = false;
            return;
        }

        // 分类筛选
        var categoryMatch = SelectedPluginType switch
        {
            PluginType.Translate => plugin.Type == "Translate",
            PluginType.Ocr => plugin.Type == "Ocr",
            PluginType.Tts => plugin.Type == "Tts",
            PluginType.Vocabulary => plugin.Type == "Vocabulary",
            _ => true
        };

        // 文本筛选
        var textMatch = string.IsNullOrEmpty(FilterText)
            || plugin.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || plugin.Author.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || plugin.Description.Contains(FilterText, StringComparison.OrdinalIgnoreCase);

        e.Accepted = categoryMatch && textMatch;
    }

    #endregion

    #region 下载和安装

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task DownloadPluginAsync(PluginMarketInfo plugin)
    {
        if (plugin.IsDownloading || plugin.ActionStatus == PluginActionStatus.Installed) return;

        // 创建新的取消令牌源
        plugin.CancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = plugin.CancellationTokenSource.Token;

        plugin.IsDownloading = true;
        plugin.DownloadProgress = 0;
        plugin.DownloadStatus = "0%";

        try
        {
            var tempPath = Path.GetTempPath();
            var fileName = $"{plugin.PackageName}.zip";

            // 创建进度报告
            var progress = new Progress<DownloadProgress>(p =>
            {
                plugin.DownloadProgress = p.Percentage;
                plugin.DownloadStatus = $"{p.Percentage:F0}% ({p.Speed / 1024:F0} KB/s)";
            });

            // 下载文件（支持取消）
            var downloadedPath = await _httpService.DownloadFileAsync(
                plugin.DownloadUrl, tempPath, fileName, progress: progress, cancellationToken: cancellationToken);

            // 检查是否已取消
            cancellationToken.ThrowIfCancellationRequested();

            // 转换为 .spkg 临时文件
            var spkgPath = Path.ChangeExtension(downloadedPath, ".spkg");
            if (File.Exists(spkgPath))
                File.Delete(spkgPath);
            File.Move(downloadedPath, spkgPath);

            // 使用现有 PluginManager 安装
            var result = _pluginService.InstallPlugin(spkgPath);

            // 处理安装结果
            await HandleInstallResultAsync(plugin, result, spkgPath);
        }
        catch (OperationCanceledException)
        {
            _snackbar.ShowWarning($"{_i18n.GetTranslation("PluginDownloadCancelled")}");
        }
        catch (Exception ex)
        {
            _snackbar.ShowError($"{_i18n.GetTranslation("PluginInstallFailed")}: {ex.Message}");
        }
        finally
        {
            plugin.IsDownloading = false;
            plugin.DownloadStatus = null;
            plugin.CancellationTokenSource?.Dispose();
            plugin.CancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelDownload(PluginMarketInfo plugin)
    {
        if (plugin.CancellationTokenSource != null && !plugin.CancellationTokenSource.IsCancellationRequested)
        {
            plugin.CancellationTokenSource.Cancel();
        }
    }

    /// <summary>
    /// 处理安装/升级结果
    /// </summary>
    private async Task HandleInstallResultAsync(PluginMarketInfo plugin,
        PluginInstallResult result, string spkgPath)
    {
        if (result.RequiredUpgrade && result.ExistingPlugin != null)
        {
            // 询问是否升级
            var dialogResult = await new ContentDialog
            {
                Title = _i18n.GetTranslation("PluginUpgrade"),
                Content = string.Format(_i18n.GetTranslation("PluginUpgradeConfirm"),
                    result.ExistingPlugin.Name,
                    result.ExistingPlugin.Version,
                    result.NewPlugin?.Version),
                PrimaryButtonText = _i18n.GetTranslation("Confirm"),
                CloseButtonText = _i18n.GetTranslation("Cancel"),
                DefaultButton = ContentDialogButton.Primary,
            }.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                // 执行升级
                if (_pluginService.UpgradePlugin(result.ExistingPlugin, spkgPath))
                {
                    _needsRestart = true;
                    _snackbar.ShowSuccess(_i18n.GetTranslation("PluginInstallSuccess"));
                    // 刷新所有插件的安装状态
                    UpdatePluginStatus();
                }
                else
                {
                    _snackbar.ShowError(_i18n.GetTranslation("PluginUpgradeFailed"));
                }
            }
        }
        else if (!result.Succeeded)
        {
            await new ContentDialog
            {
                Title = _i18n.GetTranslation("PluginInstallFailed"),
                CloseButtonText = _i18n.GetTranslation("Ok"),
                DefaultButton = ContentDialogButton.Close,
                Content = result.Message
            }.ShowAsync();
        }
        else
        {
            // 安装成功
            _snackbar.ShowSuccess(_i18n.GetTranslation("PluginInstallSuccess"));
            // 刷新所有插件的安装状态
            UpdatePluginStatus();
        }

        // 清理临时文件
        try
        {
            if (File.Exists(spkgPath))
                File.Delete(spkgPath);
        }
        catch { }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 版本比较
    /// </summary>
    private static int CompareVersions(string? localVersion, string? marketVersion)
    {
        if (string.IsNullOrEmpty(localVersion)) return -1;
        if (string.IsNullOrEmpty(marketVersion)) return 1;

        // 尝试解析为 System.Version
        if (Version.TryParse(localVersion, out var v1) && Version.TryParse(marketVersion, out var v2))
        {
            return v1.CompareTo(v2);
        }

        // 手动解析版本号
        var parts1 = ParseVersionParts(localVersion);
        var parts2 = ParseVersionParts(marketVersion);

        int maxLength = Math.Max(parts1.Length, parts2.Length);
        for (int i = 0; i < maxLength; i++)
        {
            int part1 = i < parts1.Length ? parts1[i] : 0;
            int part2 = i < parts2.Length ? parts2[i] : 0;

            int result = part1.CompareTo(part2);
            if (result != 0) return result;
        }

        return 0;
    }

    private static int[] ParseVersionParts(string version)
    {
        var cleanVersion = new string(version.Where(c => char.IsDigit(c) || c == '.').ToArray());
        return cleanVersion.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part, out var num) ? num : 0)
            .ToArray();
    }

    #endregion

    #region MetaData 类定义

    public class PluginInfo
    {
        public string PluginID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string ExecuteFileName { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 插件多语言信息（对应 Languages/zh-cn.json）
    /// </summary>
    public class PluginLanguageInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    #endregion
}

/// <summary>
/// 插件市场信息模型
/// </summary>
public partial class PluginMarketInfo : ObservableObject
{
    /// <summary>
    /// 插件唯一ID
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// 插件名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 插件作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 插件类型 (Translate/Ocr/Tts/Vocabulary)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 市场版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 插件描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 项目网址
    /// </summary>
    public string Website { get; set; } = string.Empty;

    /// <summary>
    /// 图标URL
    /// </summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>
    /// 下载URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 包名 (STranslate.Plugin.Type.Name)
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已安装
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionStatus))]
    public partial bool IsInstalled { get; set; }

    /// <summary>
    /// 是否下载中
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionStatus))]
    public partial bool IsDownloading { get; set; }

    /// <summary>
    /// 下载进度 0-100
    /// </summary>
    [ObservableProperty]
    public partial double DownloadProgress { get; set; }

    /// <summary>
    /// 下载状态文本
    /// </summary>
    [ObservableProperty]
    public partial string? DownloadStatus { get; set; }

    /// <summary>
    /// 取消令牌源（用于取消下载）
    /// </summary>
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// 是否可以升级
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionStatus))]
    public partial bool CanUpgrade { get; set; }

    /// <summary>
    /// 当前安装的版本（用于比较）
    /// </summary>
    public string? InstalledVersion { get; set; }

    /// <summary>
    /// 获取操作状态
    /// </summary>
    public PluginActionStatus ActionStatus
    {
        get
        {
            if (IsDownloading)
                return PluginActionStatus.Downloading;
            if (!IsInstalled)
                return PluginActionStatus.Download;
            if (CanUpgrade)
                return PluginActionStatus.Upgrade;
            return PluginActionStatus.Installed;
        }
    }
}

/// <summary>
/// 插件操作状态
/// </summary>
public enum PluginActionStatus
{
    /// <summary>
    /// 可下载
    /// </summary>
    Download,

    /// <summary>
    /// 已安装最新版本
    /// </summary>
    Installed,

    /// <summary>
    /// 可升级
    /// </summary>
    Upgrade,

    /// <summary>
    /// 下载中
    /// </summary>
    Downloading
}
