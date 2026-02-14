using CommunityToolkit.Mvvm.DependencyInjection;
using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Core;
using STranslate.Plugin;
using STranslate.ViewModels;
using STranslate.Views.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace STranslate.Views;

public partial class SettingsWindow
{
    private readonly SettingsWindowViewModel _viewModel;
    private bool _isCodeNavi;

    public SettingsWindow()
    {
        _viewModel = Ioc.Default.GetRequiredService<SettingsWindowViewModel>();
        DataContext = _viewModel;

        InitializeComponent();
    }

    private void OnNaviSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_isCodeNavi)
        {
            _isCodeNavi = false;
            return;
        }

        var selectedItem = args.SelectedItemContainer;
        var tag = selectedItem.Tag?.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        Navigate(tag, false);
    }

    public void Navigate(string tag, bool isCodeBehinde = true)
    {
        var content = tag switch
        {
            nameof(GeneralPage) => Ioc.Default.GetRequiredService<GeneralPage>(),
            nameof(TranslatePage) => Ioc.Default.GetRequiredService<TranslatePage>(),
            nameof(OcrPage) => Ioc.Default.GetRequiredService<OcrPage>(),
            nameof(TtsPage) => Ioc.Default.GetRequiredService<TtsPage>(),
            nameof(VocabularyPage) => Ioc.Default.GetRequiredService<VocabularyPage>(),
            nameof(StandalonePage) => Ioc.Default.GetRequiredService<StandalonePage>(),
            nameof(HistoryPage) => Ioc.Default.GetRequiredService<HistoryPage>(),
            nameof(PluginPage) => Ioc.Default.GetRequiredService<PluginPage>(),
            nameof(HotkeyPage) => Ioc.Default.GetRequiredService<HotkeyPage>(),
            nameof(NetworkPage) => Ioc.Default.GetRequiredService<NetworkPage>(),
            nameof(AboutPage) => Ioc.Default.GetRequiredService < AboutPage>(),
            _ => default(iNKORE.UI.WPF.Modern.Controls.Page)
        };
        if (isCodeBehinde)
        {
            _isCodeNavi = true;
            switch (tag)
            {
                case nameof(GeneralPage):
                    RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
                    break;
                case nameof(TranslatePage):
                    RootNavigation.SelectedItem = (RootNavigation.MenuItems[1] as NavigationViewItem)?.MenuItems[0];
                    break;
                case nameof(OcrPage):
                    RootNavigation.SelectedItem = (RootNavigation.MenuItems[1] as NavigationViewItem)?.MenuItems[1];
                    break;
                case nameof(TtsPage):
                    RootNavigation.SelectedItem = (RootNavigation.MenuItems[1] as NavigationViewItem)?.MenuItems[2];
                    break;
                case nameof(VocabularyPage):
                    RootNavigation.SelectedItem = (RootNavigation.MenuItems[1] as NavigationViewItem)?.MenuItems[3];
                    break;
                case nameof(StandalonePage):
                    RootNavigation.SelectedItem = RootNavigation.MenuItems[2];
                    break;
                case nameof(HistoryPage):
                    RootNavigation.SelectedItem = RootNavigation.MenuItems[3];
                    break;
                default:
                    break;
            }
        }
        ((INavigation)App.Current).IsNavigated = true;
        RootFrame.Content = content;
        ((INavigation)App.Current).IsNavigated = false;
    }

    private void OnKeyDown(object _, KeyEventArgs e)
    {
        if (e.Key is not Key.F || Keyboard.Modifiers is not ModifierKeys.Control) return;

        switch (RootFrame.Content)
        {
            case GeneralPage page:
                FocusAndSelectAll(page.PART_AutoSuggestBox);
                break;
            case StandalonePage page:
                FocusAndSelectAll(page.PART_AutoSuggestBox);
                break;
            case HistoryPage page:
                FocusAndSelectAll(page.PART_SearchBox);
                break;
            case PluginPage page:
                FocusAndSelectAll(page.ViewModel.IsMarketView ? page.MarketFilterTextbox : page.PluginFilterTextbox);
                break;
            case HotkeyPage page:
                FocusAndSelectAll(page.PART_AutoSuggestBox);
                break;
            case NetworkPage page:
                FocusAndSelectAll(page.PART_AutoSuggestBox);
                break;
        }
    }

    private static void FocusAndSelectAll(Control control)
    {
        control.Focus();

        if (control is TextBox textBox)
            textBox.SelectAll();
        else if (control is AutoSuggestBox autoSuggestBox)
            Utilities.FindVisualChild<TextBox>(autoSuggestBox, "TextBox")?.SelectAll();
    }
}