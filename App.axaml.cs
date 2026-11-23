using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using RPZ_DiskspaceAnalyzer.Helpers;
using RPZ_DiskspaceAnalyzer.ViewModels;
using RPZ_DiskspaceAnalyzer.Views;

namespace RPZ_DiskspaceAnalyzer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            ClipboardHelper.Initialize(desktop.MainWindow.Clipboard!);
        }

        base.OnFrameworkInitializationCompleted();
    }
}