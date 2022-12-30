using ReactiveUI;

namespace Resizer.ViewModels;

public class Config : ReactiveObject
{
    private bool _invertImageX = true;
    private bool _invertImageY = true;
    private double _defaultScale = 100;
    private double _scaleMultiplier = 2;
    
    private int _cropperWidth = 512;
    private int _cropperHeight = 512;
    
    public bool InvertImageX
    {
        get => _invertImageX;
        set => this.RaiseAndSetIfChanged(ref _invertImageX, value);
    }
    public bool InvertImageY
    {
        get => _invertImageY;
        set => this.RaiseAndSetIfChanged(ref _invertImageY, value);
    }
    public double DefaultScale
    {
        get => _defaultScale;
        set => this.RaiseAndSetIfChanged(ref _defaultScale, value);
    }
    public double ScaleMultiplier
    {
        get => _scaleMultiplier;
        set => this.RaiseAndSetIfChanged(ref _scaleMultiplier, value);
    }
    
    
    public int CropperWidth
    {
        get => _cropperWidth;
        set => this.RaiseAndSetIfChanged(ref _cropperWidth, value);
    }
    public int CropperHeight
    {
        get => _cropperHeight;
        set => this.RaiseAndSetIfChanged(ref _cropperHeight, value);
    }
    
}