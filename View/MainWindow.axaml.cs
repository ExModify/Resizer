using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DynamicData;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Resizer.Classes;
using Resizer.ViewModels;

namespace Resizer.View;

public partial class MainWindow : Window
{
    private DateTime _previousTime = DateTime.Now;
    public MainWindow()
    {
        InitializeComponent();
    }

    /*
     * Event listeners
     */

    private void OpenSrc(object? sender, RoutedEventArgs e)
    {
        new Task(OpenSrcFolder).Start();
    }

    private void OpenDst(object? sender, RoutedEventArgs e)
    {
        new Task(OpenDstFolder).Start();
    }

    private void ResetItClick(object? sender, RoutedEventArgs e)
    {
        _previousTime = DateTime.Now;
        Speed.Content = "0";
    }
    
    private void SkipClicked(object? sender, RoutedEventArgs e)
    {
        UpdateIt();
        
    }
    private void PrevClicked(object? sender, RoutedEventArgs e)
    {
        UpdateIt();
    }
    private void NextClicked(object? sender, RoutedEventArgs e) 
    {
        UpdateIt();
    }
    private void FinishClick(object? sender, RoutedEventArgs e) 
    {
        _previousTime = DateTime.Now;

        string path = DstFolder.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            DstFolder.Text = path = Path.TrimEndingDirectorySeparator(SrcFolder.Text) + "_cropped";
        }

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        
        MainWindowViewModel model = (MainWindowViewModel)DataContext!;
        for (int i = 0; i < model.Croppers.Count; i++)
        {
            model.Croppers[i].Save(Path.Join(path,
                FileList.SelectedIndex.ToString("D6") + "-" + i.ToString("D3") + ".png"));
        }
    }
    
    /*
     * Helper methods
     */

    private void UpdateIt()
    {
        TimeSpan span = DateTime.Now - _previousTime;
        _previousTime = DateTime.Now;

        double it = TimeSpan.FromSeconds(1) / span;
        
        Speed.Content = it.ToString("N2");
    }
    private async Task OpenFolderAsync(TextBox target)
    {
        OpenFolderDialog dialog = new();
        string? value = await dialog.ShowAsync(this);

        if (value != null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                target.Text = value;
            });
        }
    }
    private async void OpenSrcFolder()
    {
        await OpenFolderAsync(SrcFolder);
        await ReadDirAsync();
        
    }
    private async void OpenDstFolder()
    {
        await OpenFolderAsync(DstFolder);
    }

    private async Task ReadDirAsync()
    {
        string path = SrcFolder.Text;
        if (string.IsNullOrWhiteSpace(path)) return;
        
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        HashSet<string> patterns = new(codecs.SelectMany(codec =>
            codec.FilenameExtension.ToLower().Replace("*", "").Split(';')));
        
        List<string> paths = Directory.EnumerateFileSystemEntries(path)
            .Where(t => patterns.Contains(Path.GetExtension(t).ToLower())).ToList();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            MainWindowViewModel model = (MainWindowViewModel)DataContext!;
            model.Files.Clear();
            model.Files.AddRange(paths);
            
            if (paths.Count > 0)
                OpenImage(0);
        });
    }

    private void ListDoubleTapped(object? sender, RoutedEventArgs e)
    {
        ListBox box = (ListBox)sender!;
        if (box.SelectedIndex < 0) return;

        OpenImage();
    }

    private void OpenImage(int? index = null)
    {
        MainWindowViewModel model = (MainWindowViewModel)DataContext!;
        while (model.Croppers.Count > 2) model.Croppers.RemoveAt(1);

        if (index.HasValue) FileList.SelectedIndex = index.Value;

        model.Croppers[0].File = model.Files[FileList.SelectedIndex];
    }

    private void CreateNewTab(object? sender, RoutedEventArgs e)
    {
        MainWindowViewModel model = (MainWindowViewModel)DataContext!;
        string newHeading = AdjustHeadings();
        model.Croppers.Add(new Cropper(model.Croppers[0], newHeading));
    }
    
    private void DeleteTab(object? sender, RoutedEventArgs e)
    {
        Button btn = (Button)sender!;
        TextBlock heading = (TextBlock)((StackPanel)btn.Parent!).Children[0];
        
        MainWindowViewModel model = (MainWindowViewModel)DataContext!;
        for (int i = 0; i < model.Croppers.Count; i++)
        {
            if (model.Croppers[i].Heading != heading.Text) continue;
            
            model.Croppers.RemoveAt(i);
            break;
        }

        AdjustHeadings();
    }

    private string AdjustHeadings()
    {
        MainWindowViewModel model = (MainWindowViewModel)DataContext!;
        int i = 0;
        for (; i < model.Croppers.Count; i++)
        {
            model.Croppers[i].Heading = (i + 1) + ".";
        }

        return (i + 1) + ".";
    }

    private void SrcFolderKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && Directory.Exists(SrcFolder.Text))
        {
            Task.Run(ReadDirAsync);
        }
    }
}