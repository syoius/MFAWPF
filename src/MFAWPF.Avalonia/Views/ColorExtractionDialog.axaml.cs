using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MFAWPF.Core.Extensions;
using MFAWPF.Core.Services;

namespace MFAWPF.Avalonia.Views;

public partial class ColorExtractionDialog : Window
{
    private Point _startPoint;
    private Rectangle? _selectionRectangle;
    private List<int>? _outputRoi;
    private double _scaleRatio;

    public List<int>? OutputRoi
    {
        get => _outputRoi;
        set => _outputRoi = value?.Select(i => i < 0 ? 0 : i).ToList();
    }

    public List<int>? OutputUpper { get; set; }
    public List<int>? OutputLower { get; set; }

    public ColorExtractionDialog()
    {
        InitializeComponent();
        Task.Run(() =>
        {
            var image = MaaProcessor.Instance.GetBitmapImage();
            Dispatcher.UIThread.InvokeAsync(() => UpdateImage(image));
        });
    }

    public void UpdateImage(Bitmap? imageSource)
    {
        if (imageSource == null)
            return;

        this.FindControl<ProgressBar>("LoadingCircle")!.IsVisible = false;
        this.FindControl<Viewbox>("ImageArea")!.IsVisible = true;
        var imageControl = this.FindControl<Image>("image")!;
        imageControl.Source = imageSource;

        double imageWidth = imageSource.PixelSize.Width;
        double imageHeight = imageSource.PixelSize.Height;

        double maxWidth = imageControl.MaxWidth ?? 1280;
        double maxHeight = imageControl.MaxHeight ?? 720;

        double widthRatio = maxWidth / imageWidth;
        double heightRatio = maxHeight / imageHeight;
        _scaleRatio = Math.Min(widthRatio, heightRatio);

        imageControl.Width = imageWidth * _scaleRatio;
        imageControl.Height = imageHeight * _scaleRatio;

        var canvas = this.FindControl<Canvas>("SelectionCanvas")!;
        canvas.Width = imageControl.Width;
        canvas.Height = imageControl.Height;
        Width = imageControl.Width + 20;
        Height = imageControl.Height + 100;

        Position = new PixelPoint(
            (int)((Screens.Primary.Bounds.Width - Width) / 2),
            (int)((Screens.Primary.Bounds.Height - Height) / 2));
    }

    private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var canvas = sender as Canvas;
        var imageControl = this.FindControl<Image>("image")!;
        var position = e.GetPosition(imageControl);
        var canvasPosition = e.GetPosition(canvas!);

        if (canvasPosition.X < imageControl.Bounds.Width + 5 && 
            canvasPosition.Y < imageControl.Bounds.Height + 5 &&
            canvasPosition is { X: >= -5, Y: >= -5 })
        {
            if (_selectionRectangle != null)
            {
                canvas.Children.Remove(_selectionRectangle);
            }

            if (position.X < 0) position = position.WithX(0);
            if (position.Y < 0) position = position.WithY(0);
            if (position.X > imageControl.Bounds.Width) 
                position = position.WithX(imageControl.Bounds.Width);
            if (position.Y > imageControl.Bounds.Height) 
                position = position.WithY(imageControl.Bounds.Height);

            _startPoint = position;

            _selectionRectangle = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2.5,
                StrokeDashArray = new AvaloniaList<double> { 2 }
            };

            Canvas.SetLeft(_selectionRectangle, _startPoint.X);
            Canvas.SetTop(_selectionRectangle, _startPoint.Y);

            canvas.Children.Add(_selectionRectangle);
            e.Pointer.Capture(canvas);
        }
    }

    private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        var imageControl = this.FindControl<Image>("image")!;
        var position = e.GetPosition(imageControl);
        var textBlock = this.FindControl<TextBlock>("MousePositionText")!;
        textBlock.Text = $"[ {(int)(position.X / _scaleRatio)}, {(int)(position.Y / _scaleRatio)} ]";

        if (_selectionRectangle == null || !e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            return;

        var canvas = sender as Canvas;
        var pos = e.GetPosition(canvas!);

        var x = Math.Min(pos.X, _startPoint.X);
        var y = Math.Min(pos.Y, _startPoint.Y);

        var w = Math.Abs(pos.X - _startPoint.X);
        var h = Math.Abs(pos.Y - _startPoint.Y);

        if (x < 0)
        {
            x = 0;
            w = _startPoint.X;
        }

        if (y < 0)
        {
            y = 0;
            h = _startPoint.Y;
        }

        if (x + w > canvas!.Bounds.Width)
        {
            w = canvas.Bounds.Width - x;
        }

        if (y + h > canvas.Bounds.Height)
        {
            h = canvas.Bounds.Height - y;
        }

        _selectionRectangle.Width = w;
        _selectionRectangle.Height = h;

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);

        textBlock.Text = 
            $"[ {(int)(x / _scaleRatio)}, {(int)(y / _scaleRatio)}, {(int)(w / _scaleRatio)}, {(int)(h / _scaleRatio)} ]";
    }

    private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_selectionRectangle == null)
            return;

        e.Pointer.Capture(null);
    }

    private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_selectionRectangle == null)
        {
            NotificationService.Show("警告", "请选择一个区域", NotificationType.Warning);
            return;
        }

        var x = Canvas.GetLeft(_selectionRectangle);
        var y = Canvas.GetTop(_selectionRectangle);
        var w = _selectionRectangle.Width;
        var h = _selectionRectangle.Height;

        GetColorRange(Math.Round(x), Math.Round(y), Math.Round(w), Math.Round(h));
        Close(true);
    }

    private void GetColorRange(double x, double y, double width, double height)
    {
        if (width < 1 || !double.IsNormal(width)) width = 1;
        if (height < 1 || !double.IsNormal(height)) height = 1;

        var imageControl = this.FindControl<Image>("image")!;
        if (imageControl.Source is Bitmap bitmap)
        {
            var roiX = Math.Max(x - 5, 0);
            var roiY = Math.Max(y - 5, 0);
            var roiW = Math.Min(width + 10, bitmap.PixelSize.Width - roiX);
            var roiH = Math.Min(height + 10, bitmap.PixelSize.Height - roiY);

            OutputRoi = [(int)roiX, (int)roiY, (int)roiW, (int)roiH];

            using var croppedBitmap = new CroppedBitmap(bitmap, 
                new PixelRect((int)x, (int)y, (int)width, (int)height));

            var pixels = new byte[(int)width * (int)height * 4];
            croppedBitmap.CopyPixels(pixels, (int)width * 4, 0);

            int minR = 255, minG = 255, minB = 255;
            int maxR = 0, maxG = 0, maxB = 0;

            for (int i = 0; i < pixels.Length; i += 4)
            {
                int r = pixels[i + 2];
                int g = pixels[i + 1];
                int b = pixels[i];

                minR = Math.Min(minR, r);
                minG = Math.Min(minG, g);
                minB = Math.Min(minB, b);

                maxR = Math.Max(maxR, r);
                maxG = Math.Max(maxG, g);
                maxB = Math.Max(maxB, b);
            }

            OutputLower = [minR, minG, minB];
            OutputUpper = [maxR, maxG, maxB];
        }
    }

    private async void Load(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "LoadFileTitle".GetLocalizationString(),
            Filters = new List<FileDialogFilter>
            {
                new() { Name = "AllFilter".GetLocalizationString(), Extensions = new List<string> { "*" } }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0)
        {
            try
            {
                var bitmap = new Bitmap(result[0]);
                UpdateImage(bitmap);
            }
            catch (Exception ex)
            {
                NotificationService.Show("错误", "加载图片失败：" + ex.Message, NotificationType.Error);
            }
        }
    }
}