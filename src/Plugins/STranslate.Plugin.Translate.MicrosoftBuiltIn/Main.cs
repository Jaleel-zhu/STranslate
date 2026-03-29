using STranslate.Plugin.Translate.MicrosoftBuiltIn.View;
using STranslate.Plugin.Translate.MicrosoftBuiltIn.ViewModel;
using System.Text.Json.Nodes;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.MicrosoftBuiltIn;

public class Main : TranslatePluginBase
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    private const string AuthUrl = "https://edge.microsoft.com/translate/auth";
    private const string ApiEndpoint = "api-edge.cognitive.microsofttranslator.com";
    private const string ApiVersion = "3.0";
    private const int MaxTextLength = 1000;

    public override Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel();
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    /// <summary>
    ///     https://learn.microsoft.com/zh-cn/azure/ai-services/translator/language-support
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    public override string? GetSourceLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "zh-Hans",
        LangEnum.ChineseTraditional => "zh-Hant",
        LangEnum.Cantonese => null,
        LangEnum.English => "en",
        LangEnum.Japanese => "ja",
        LangEnum.Korean => "ko",
        LangEnum.French => "fr",
        LangEnum.Spanish => "es",
        LangEnum.Russian => "ru",
        LangEnum.German => "de",
        LangEnum.Italian => "it",
        LangEnum.Turkish => "tr",
        LangEnum.PortuguesePortugal => "pt-pt",
        LangEnum.PortugueseBrazil => "pt",
        LangEnum.Vietnamese => "vi",
        LangEnum.Indonesian => "id",
        LangEnum.Thai => "th",
        LangEnum.Malay => "ms",
        LangEnum.Arabic => "ar",
        LangEnum.Hindi => null,
        LangEnum.MongolianCyrillic => "mn-Cyrl",
        LangEnum.MongolianTraditional => "mn-Mong",
        LangEnum.Khmer => "km",
        LangEnum.NorwegianBokmal => "nb",
        LangEnum.NorwegianNynorsk => "nb",
        LangEnum.Persian => "fa",
        LangEnum.Swedish => "sv",
        LangEnum.Polish => "pl",
        LangEnum.Dutch => "nl",
        LangEnum.Ukrainian => "uk",
        _ => "auto"
    };

    public override string? GetTargetLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "zh-Hans",
        LangEnum.ChineseTraditional => "zh-Hant",
        LangEnum.Cantonese => null,
        LangEnum.English => "en",
        LangEnum.Japanese => "ja",
        LangEnum.Korean => "ko",
        LangEnum.French => "fr",
        LangEnum.Spanish => "es",
        LangEnum.Russian => "ru",
        LangEnum.German => "de",
        LangEnum.Italian => "it",
        LangEnum.Turkish => "tr",
        LangEnum.PortuguesePortugal => "pt-pt",
        LangEnum.PortugueseBrazil => "pt",
        LangEnum.Vietnamese => "vi",
        LangEnum.Indonesian => "id",
        LangEnum.Thai => "th",
        LangEnum.Malay => "ms",
        LangEnum.Arabic => "ar",
        LangEnum.Hindi => null,
        LangEnum.MongolianCyrillic => "mn-Cyrl",
        LangEnum.MongolianTraditional => "mn-Mong",
        LangEnum.Khmer => "km",
        LangEnum.NorwegianBokmal => "nb",
        LangEnum.NorwegianNynorsk => "nb",
        LangEnum.Persian => "fa",
        LangEnum.Swedish => "sv",
        LangEnum.Polish => "pl",
        LangEnum.Dutch => "nl",
        LangEnum.Ukrainian => "uk",
        _ => "auto"
    };

    public override void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public override void Dispose() { }

    public override async Task TranslateAsync(TranslateRequest request, TranslateResult result, CancellationToken cancellationToken = default)
    {
        if (GetSourceLanguage(request.SourceLang) is not string sourceStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedSourceLang"));
            return;
        }
        if (GetTargetLanguage(request.TargetLang) is not string targetStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedTargetLang"));
            return;
        }

        var token = await Context.HttpService.GetAsync(AuthUrl, new Options(), cancellationToken);
        token = token.Trim().Trim('"');
        string url = $"https://{ApiEndpoint}/translate?api-version={ApiVersion}&to={targetStr}";
        if (!string.IsNullOrEmpty(sourceStr) && sourceStr != "auto")
        {
            url += $"&from={sourceStr}";
        }
        
        var content = new[] { new { request.Text } };
        var options = new Options
        {
            Headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {token}" }
            }
        };

        var response = await Context.HttpService.PostAsync(url, content, options, cancellationToken);
        var rootNode = JsonNode.Parse(response);
        if (rootNode is JsonArray arr && arr.Count > 0)
        {
            var translations = arr[0]?["translations"] as JsonArray;
            var data = translations?[0]?["text"]?.ToString() ?? throw new Exception($"No result.\nRaw: {response}");
            result.Success(data);
        }
        else
        {
            throw new Exception($"No result.\nRaw: {response}");
        }
    }
}
