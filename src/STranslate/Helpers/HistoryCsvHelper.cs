using STranslate.Core;
using STranslate.Plugin;
using System.Text;

namespace STranslate.Helpers;

/// <summary>
/// 负责把历史记录转换为可读的宽表 CSV 文本。
/// </summary>
public static class HistoryCsvHelper
{
    private static readonly char[] CsvEscapeChars = [',', '"', '\r', '\n'];

    /// <summary>
    /// 统一的 CSV 输出编码（UTF-8 with BOM），用于兼容 Excel 打开中文。
    /// </summary>
    public static readonly Encoding Utf8BomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>
    /// 构建历史记录 CSV 文本。
    /// </summary>
    /// <param name="items">要导出的历史记录。</param>
    /// <param name="services">当前已加载服务，用于解析引擎显示名。</param>
    /// <param name="languageDisplayNameResolver">语言代码到显示名的解析委托。</param>
    /// <returns>完整 CSV 字符串。</returns>
    public static string BuildCsv(
        IReadOnlyList<HistoryModel> items,
        IEnumerable<Service> services,
        Func<string?, string> languageDisplayNameResolver)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(languageDisplayNameResolver);

        var serviceNameMap = BuildServiceNameMap(services);
        var engineRows = items.Select(history => BuildExportEngineItems(history, serviceNameMap)).ToList();
        var maxEngineCount = engineRows.Count == 0 ? 0 : engineRows.Max(row => row.Count);

        var headers = new List<string>
        {
            "序号",
            "记录ID",
            "时间",
            "原文语言",
            "目标语言",
            "翻译原文"
        };

        for (var index = 1; index <= maxEngineCount; index++)
        {
            headers.Add($"翻译引擎{index}");
            headers.Add($"翻译结果{index}");
        }

        var csvBuilder = new StringBuilder();
        AppendCsvRow(csvBuilder, headers);

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var row = new List<string>
            {
                (index + 1).ToString(),
                item.Id.ToString(),
                item.Time.ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
                languageDisplayNameResolver(ResolveLanguageForExport(item.EffectiveSourceLang, item.SourceLang)),
                languageDisplayNameResolver(ResolveLanguageForExport(item.EffectiveTargetLang, item.TargetLang)),
                item.SourceText ?? string.Empty
            };

            var engineItems = engineRows[index];
            for (var engineIndex = 0; engineIndex < maxEngineCount; engineIndex++)
            {
                if (engineIndex < engineItems.Count)
                {
                    row.Add(engineItems[engineIndex].EngineName);
                    row.Add(engineItems[engineIndex].ResultText);
                }
                else
                {
                    row.Add(string.Empty);
                    row.Add(string.Empty);
                }
            }

            AppendCsvRow(csvBuilder, row);
        }

        return csvBuilder.ToString();
    }

    private static IReadOnlyDictionary<string, string> BuildServiceNameMap(IEnumerable<Service> services)
    {
        return services
            .GroupBy(service => BuildServiceKey(service.MetaData.PluginID, service.ServiceID), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().DisplayName, StringComparer.Ordinal);
    }

    private static List<ExportEngineItem> BuildExportEngineItems(
        HistoryModel history,
        IReadOnlyDictionary<string, string> serviceNameMap)
    {
        var result = new List<ExportEngineItem>();

        foreach (var data in history.Data)
        {
            var engineName = ResolveEngineName(data, serviceNameMap);
            var resultText = BuildResultText(data);
            result.Add(new ExportEngineItem(engineName, resultText));
        }

        return result;
    }

    private static string ResolveEngineName(HistoryData data, IReadOnlyDictionary<string, string> serviceNameMap)
    {
        var key = BuildServiceKey(data.PluginID, data.ServiceID);
        if (serviceNameMap.TryGetValue(key, out var displayName) && !string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return string.IsNullOrWhiteSpace(data.ServiceID) ? "Unknown" : $"Unknown({data.ServiceID})";
    }

    private static string BuildResultText(HistoryData data)
    {
        if (data.DictResult != null)
            return BuildDictionarySummary(data.DictResult);

        return BuildTranslationText(data);
    }

    /// <summary>
    /// 失败时统一留空，避免把错误文案写入导出结果，方便后续筛选有效译文。
    /// </summary>
    private static string BuildTranslationText(HistoryData data)
    {
        if (data.TransResult is not { IsSuccess: true } transResult || string.IsNullOrWhiteSpace(transResult.Text))
            return string.Empty;

        var mainText = transResult.Text;
        if (data.TransBackResult is { IsSuccess: true } backResult && !string.IsNullOrWhiteSpace(backResult.Text))
            return $"{mainText}{Environment.NewLine}回译: {backResult.Text}";

        return mainText;
    }

    private static string BuildDictionarySummary(DictionaryResult dictResult)
    {
        if (dictResult.ResultType != DictionaryResultType.Success)
            return string.Empty;

        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(dictResult.Text))
            lines.Add($"词条: {dictResult.Text.Trim()}");

        var meanings = BuildDictionaryMeanText(dictResult);
        if (!string.IsNullOrWhiteSpace(meanings))
            lines.Add($"释义: {meanings}");

        var sentences = string.Join(
            Environment.NewLine,
            dictResult.Sentences.Where(sentence => !string.IsNullOrWhiteSpace(sentence))
        );
        if (!string.IsNullOrWhiteSpace(sentences))
            lines.Add($"例句: {sentences}");

        return lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
    }

    private static string BuildDictionaryMeanText(DictionaryResult dictResult)
    {
        var lines = dictResult.DictMeans
            .Select(mean =>
            {
                var partOfSpeech = mean.PartOfSpeech?.Trim() ?? string.Empty;
                var means = string.Join("；", mean.Means.Where(m => !string.IsNullOrWhiteSpace(m)));

                if (string.IsNullOrWhiteSpace(partOfSpeech) && string.IsNullOrWhiteSpace(means))
                    return string.Empty;
                if (string.IsNullOrWhiteSpace(partOfSpeech))
                    return means;
                if (string.IsNullOrWhiteSpace(means))
                    return partOfSpeech;

                return $"{partOfSpeech} {means}";
            })
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildServiceKey(string pluginId, string serviceId)
        => $"{pluginId}|{serviceId}";

    private static string? ResolveLanguageForExport(string? effectiveLanguage, string? fallbackLanguage)
        => string.IsNullOrWhiteSpace(effectiveLanguage) ? fallbackLanguage : effectiveLanguage;

    private static void AppendCsvRow(StringBuilder csvBuilder, IReadOnlyList<string> fields)
    {
        csvBuilder.AppendJoin(',', fields.Select(EscapeCsvField));
        csvBuilder.Append("\r\n");
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.IndexOfAny(CsvEscapeChars) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private readonly record struct ExportEngineItem(string EngineName, string ResultText);
}
