using System;
using System.Net.WebSockets;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Resizer.View;
using Resizer.ViewModels;

namespace Resizer;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ResizerConfig.LoadConfig();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow window = new();
            MainWindowViewModel ctx = new();
            window.DataContext = ctx;
            
            desktop.MainWindow = window;
            
            ctx.Croppers.CollectionChanged += (s, e) =>
            {
                ctx.UpdateIsLast();
            };
            
        }

        base.OnFrameworkInitializationCompleted();
    }
}