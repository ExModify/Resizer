using System;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Visuals.Media.Imaging;
using ReactiveUI;

namespace Resizer.Classes;

public class Cropper : ReactiveObject
{
    private Image _imageControl;
    private Rectangle _rectangleControl;

    private readonly TranslateTransform _translate;
    private readonly ScaleTransform _scale;
    
    private string _heading;
    private string? _file;
    private Bitmap? _image;

    private string? _dump;
    private bool _draggingImage;
    private bool _draggingCropper;
    private double _prevX;
    private double _prevY;

    private static int CropperWidth => ResizerConfig.CropperWidth;
    private static int CropperHeight => ResizerConfig.CropperHeight;
    private static double CropperAr => CropperWidth / (double)CropperHeight;

    private double ImageAr => _image == null ? 0 : _image.Size.Width / _image.Size.Height;
    private double ControlAr => _imageControl.Parent!.Bounds.Width / _imageControl.Parent!.Bounds.Height;

    private Rect RenderedImageBounds
    {
        get
        {
            if (_image == null || _imageControl.Parent == null) return new Rect(0, 0, 0, 0);

            double width = _image.Size.Width * (Scale / 100);
            double height = _image.Size.Height * (Scale / 100);

            double x = (_imageControl.Parent.Bounds.Width - width) / 2 + _translate.X;
            double y = (_imageControl.Parent.Bounds.Height - height) / 2 + _translate.Y;
            
            return new Rect(x, y, width, height);
        }
    }

    private double DefaultRenderScale
    {
        get
        {
            if (_imageControl.Parent == null || _image == null) return 0;
            
            if (ImageAr > ControlAr)
            {
                return _imageControl.Parent.Bounds.Width / _image.Size.Width;
            }
            return _imageControl.Parent.Bounds.Height / _image.Size.Height;
        }
    }
    
    public string Heading
    {
        get => _heading;
        set => this.RaiseAndSetIfChanged(ref _heading, value);
    }
    private Bitmap? Image
    {
        get => _image;
        set
        {
            this.RaiseAndSetIfChanged(ref _image, value);
            ImageControl.Source = _image;

            UpdateSizes();

            Width = "";
            Height = "";
            Scale = DefaultRenderScale * 100;
        }
    }
    
    public string Width
    {
        get => _image == null ? "0px" : _image.Size.Width + "px";
        set
        {
            this.RaiseAndSetIfChanged(ref _dump, value);
            _dump = null;
        }
    }
    public string Height
    {
        get => _image == null ? "0px" : _image.Size.Height + "px";
        set
        {
            this.RaiseAndSetIfChanged(ref _dump, value);
            _dump = null;
        }
    }
    public string? File
    {
        get => _file;
        set
        {
            Image = Convert(value);
            _file = value;
        }
    }
    
    public Image ImageControl
    {
        get => _imageControl;
        set => this.RaiseAndSetIfChanged(ref _imageControl, value);
    }
    public Rectangle RectangleControl
    {
        get => _rectangleControl;
        set => this.RaiseAndSetIfChanged(ref _rectangleControl, value);
    }

    public double Scale
    {
        get => DefaultRenderScale * _scale.ScaleY * 100;
        set
        {
            double val = Math.Max(value / DefaultRenderScale / 100, 1);
            
            _scale.ScaleY = val;
            _scale.ScaleX = val;
            
            this.RaisePropertyChanged();
            ToBeCroppedWidth = "";
            ToBeCroppedHeight = "";
            
            MoveImage(0, 0);
            MoveCropper(0, 0);
        }
    }
    public double OffsetX 
    {
        get => _translate.X;
        set
        {
            if (!(Math.Abs(_translate.X - value) > 0.001)) return;
            
            _translate.X = value;
            this.RaisePropertyChanged();
        }
    }
    public double OffsetY
    {
        get => _translate.Y;
        set
        {
            if (!(Math.Abs(_translate.Y - value) > 0.00001)) return;
            
            _translate.Y = value;
            this.RaisePropertyChanged();
        }
    }

    public string ToBeCroppedWidth
    {
        get => (_rectangleControl.Width * (1 / (Scale / 100))).ToString("N2") + "px";
        set
        {
            _ = value;
            this.RaisePropertyChanged();
        }
    }

    public string ToBeCroppedHeight
    {
        get => (_rectangleControl.Height * (1 / (Scale / 100))).ToString("N2") + "px";
        set
        {
            _ = value;
            this.RaisePropertyChanged();
        }
    }
    
    public Cropper(Cropper cropper, string newHeading) : this(newHeading)
    {
        Image = cropper.Image;
        Scale = cropper.Scale;
        OffsetX = cropper.OffsetX;
        OffsetY = cropper.OffsetY;
        UpdateSizes();
    }
    public Cropper(string heading, string? image = null)
    {
        _heading = heading;
        _file = image;
        _image = Convert(image);

        _scale = new ScaleTransform(1, 1);
        _translate = new TranslateTransform(0, 0);

        TransformGroup group = new();
        group.Children.Add(_scale);
        group.Children.Add(_translate);
        
        _imageControl = new Image()
        {
            Source = _image,
            RenderTransform = group,
            RenderTransformOrigin = RelativePoint.Center
        };
        _rectangleControl = new Rectangle()
        {
            StrokeThickness = 2,
            Stroke = Brushes.Red,
            Width = 100,
            Height = 100,
            IsVisible = true,
            Margin = new Thickness(0, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        UpdateSizes();

        Scale *= ResizerConfig.DefaultScale / 100;

        ImageControl.PropertyChanged += AttachListeners;
    }

    private void UpdateSizes()
    {
        if (_image == null)
        {
            _rectangleControl.IsVisible = false;
            return;
        }

        if (_imageControl.Parent == null)
        {
            
            return;
        }
        
        double renderScale = _imageControl.Parent.Bounds.Height / _image.Size.Height;
        if (ImageAr > ControlAr)
            renderScale = _imageControl.Parent.Bounds.Width / _image.Size.Width;
        
        double size = _image.Size.Height * renderScale;
        if (ImageAr < CropperAr)
            size = _image.Size.Width * renderScale;

        
        _rectangleControl.Width = _rectangleControl.Height = size;
        
        double centerX = _imageControl.Parent.Bounds.Width / 2;
        double centerY = _imageControl.Parent.Bounds.Height / 2;

        _rectangleControl.Margin = new Thickness(centerX - size / 2,
                                                centerY - size / 2, 0, 0);
        _rectangleControl.IsVisible = true;
    }

    private void ImageMouseWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y == 0)
            Scale += e.Delta.X * ResizerConfig.ScaleMultiplier;
        else
            Scale += e.Delta.Y * ResizerConfig.ScaleMultiplier;
    }

    private void ImageMouseUp(object? sender, PointerReleasedEventArgs e)
    {
        switch (e.InitialPressMouseButton)
        {
            case MouseButton.Right:
                _draggingImage = false;
                break;
            case MouseButton.Left:
                _draggingCropper = false;
                break;
        }
    }

    public void Save(string savePath)
    {
        if (_image == null || _imageControl.Parent == null) return;
        
        RenderTargetBitmap dst = new(new PixelSize(CropperWidth, CropperHeight));

        double scale = Scale / 100;
        double invertedScale = 1 / scale;
        
        // First get the default image offset from the center (container - actualWidth / 2), then shift by translate
        double imageOffsetX = (_imageControl.Parent.Bounds.Width - _image.Size.Width * scale) / 2 + _translate.X;
        double imageOffsetY = (_imageControl.Parent.Bounds.Height - _image.Size.Height * scale) / 2 + _translate.Y;
        
        // Remove the image offset from the cropper's position, whose coordinates are relative to the display viewport
        // Then scale it up to match the image's viewport
        double x = (_rectangleControl.Margin.Left - imageOffsetX) * invertedScale;
        double y = (_rectangleControl.Margin.Top - imageOffsetY) * invertedScale;
        
        // Scale the cropper's width and height from display to 
        double width = _rectangleControl.Width * invertedScale;
        double height = _rectangleControl.Height * invertedScale;

        using (IDrawingContextImpl ctx = dst.CreateDrawingContext(null))
        {
            ctx.DrawBitmap(_image.PlatformImpl, 1, new Rect(x, y, width, height),
                new Rect(0, 0, dst.Size.Width, dst.Size.Height), BitmapInterpolationMode.HighQuality);
        }
        
        dst.Save(savePath);
    }

    private void ImageMouseDown(object? sender, PointerPressedEventArgs e)
    {
        TabControl tabs = ((sender as Control)?.Parent?.Parent! as TabControl)!;
        if (!_heading.StartsWith(tabs.SelectedIndex + 1 + "")) return;
            
        PointerPoint point = e.GetCurrentPoint(sender as Control);
        if (point.Properties is { IsRightButtonPressed: false, IsLeftButtonPressed: false }) return;
        
        _draggingImage = point.Properties.IsRightButtonPressed;
        _draggingCropper = point.Properties.IsLeftButtonPressed;
        _prevX = point.Position.X;
        _prevY = point.Position.Y;
    }
    
    private void ImageMouseMove(object? sender, PointerEventArgs e)
    {
        if (!_draggingImage && !_draggingCropper) return;
        
        PointerPoint point = e.GetCurrentPoint(sender as Control);
        
        double offsetX = _prevX - point.Position.X;
        double offsetY = _prevY - point.Position.Y;
        
        _prevX = point.Position.X;
        _prevY = point.Position.Y;
            
        if (_draggingImage) MoveImage(offsetX, offsetY);
        if (_draggingCropper) MoveCropper(offsetX, offsetY);
    }

    private void MoveImage(double offsetX, double offsetY)
    {
        if (_imageControl.Parent == null) return;
        
        if (ResizerConfig.InvertImageX)
            offsetX *= -1;
        
        if (ResizerConfig.InvertImageY)
            offsetY *= -1;
        
        Rect image = RenderedImageBounds;

        double futureX = offsetX + image.X;
        double futureY = offsetY + image.Y;

        if (image.Width > _imageControl.Parent.Bounds.Width)
        {
            
            switch (futureX)
            {
                case <= 0 when futureX + image.Width >= _imageControl.Parent.Bounds.Width:
                    OffsetX += offsetX;
                    break;
                case > 0:
                    OffsetX -= image.X;
                    break;
                default:
                    OffsetX = (_imageControl.Parent.Bounds.Width - image.Width) / 2;
                    break;
            }
        }
        else
        {
            OffsetX = 0;
        }

        if (image.Height > _imageControl.Bounds.Height)
        {
            switch (futureY)
            {
                case <= 0 when futureY + image.Height >= _imageControl.Parent.Bounds.Height:
                    OffsetY += offsetY;
                    break;
                case > 0:
                    OffsetY -= image.Y;
                    break;
                default:
                    OffsetY = (_imageControl.Parent.Bounds.Height - image.Height) / 2;
                    break;
            }
        }
        else
        {
            OffsetY = 0;
        }
        
        double moveX = 0, moveY = 0;
        Rect r = new(_rectangleControl.Margin.Left, _rectangleControl.Margin.Top,
            _rectangleControl.Width, _rectangleControl.Height);
        image = RenderedImageBounds;
        
        if (!(image.X <= r.X && image.X + image.Width >= r.X + r.Width))
            moveX = offsetX;
        if (!(image.Y <= r.Y && image.Y + image.Height >= r.Y + r.Height))
            moveY = offsetY;

        if (moveX != 0 || moveY != 0)
        {
            MoveCropper(moveX, moveY);
        }
    }
    private void MoveCropper(double offsetX, double offsetY)
    {
        double left, top;
        
        if (ResizerConfig.InvertImageX)
            left = RectangleControl.Margin.Left - offsetX;
        else 
            left = RectangleControl.Margin.Left + offsetX;
        
        if (ResizerConfig.InvertImageY)
            top = RectangleControl.Margin.Top - offsetY;
        else 
            top = RectangleControl.Margin.Top + offsetY;

        Rect r = new(left, top, _rectangleControl.Width, _rectangleControl.Height);
        Rect image = RenderedImageBounds;
        
        if (!(image.X <= r.X && image.X + image.Width >= r.X + r.Width))
            left = RectangleControl.Margin.Left;
        if (!(image.Y <= r.Y && image.Y + image.Height >= r.Y + r.Height))
            top = RectangleControl.Margin.Top;
        
        r = new Rect(left, top, _rectangleControl.Width, _rectangleControl.Height);
        
        if (!(image.X <= r.X && image.X + image.Width >= r.X + r.Width))
            left = image.X;
        if (!(image.Y <= r.Y && image.Y + image.Height >= r.Y + r.Height))
            top = image.Y;
        
        RectangleControl.Margin = new Thickness(left, top,
            _rectangleControl.Margin.Right, _rectangleControl.Margin.Bottom);
    }

    private static Bitmap? Convert(string? value)
    {
        if (value == null) return null;
        
        try
        {
            Bitmap b = new(value);
            if (b == null) throw new Exception();
            return b;
        }
        catch
        {
            Uri uri;

            if (value.StartsWith("avares://"))
            {
                uri = new Uri(value);
            }
            else
            {
                string assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
                uri = new Uri($"avares://{assemblyName}{value}");
            }

            IAssetLoader assets = AvaloniaLocator.Current.GetService<IAssetLoader>()!;
            try
            {
                Stream asset = assets.Open(uri);
                return new Bitmap(asset);
            }
            catch
            {
                return null;
            }
        }
    }

    private void AttachListeners(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (_imageControl.Parent?.Parent == null) return;
        
        _imageControl.Parent.Parent.PointerPressed += ImageMouseDown;
        _imageControl.Parent.Parent.PointerReleased += ImageMouseUp;
        _imageControl.Parent.Parent.PointerMoved += ImageMouseMove;
        _imageControl.Parent.Parent.PointerWheelChanged += ImageMouseWheel;

        ImageControl.PropertyChanged -= AttachListeners;
    }

}