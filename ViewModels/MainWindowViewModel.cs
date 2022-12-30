using System.Collections.ObjectModel;
using Avalonia.Controls;
using ReactiveUI;
using Resizer.Classes;

namespace Resizer.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private bool _isLast = true;
    
    private ObservableCollection<string> _files = new();
    private ObservableCollection<Cropper> _croppers = new()
    {
        new Cropper("1.")
    };

    public ObservableCollection<string> Files
    {
        get => _files;
        set => this.RaiseAndSetIfChanged(ref _files, value);
    }
    public ObservableCollection<Cropper> Croppers
    {
        get => _croppers;
        set
        {
            this.RaiseAndSetIfChanged(ref _croppers, value);
            UpdateIsLast();
        }
    }

    public bool IsLast
    {
        get => _isLast;
        set => this.RaiseAndSetIfChanged(ref _isLast, value);
    }

    public void UpdateIsLast()
    {
        IsLast = _croppers.Count == 1;
    }
    
}