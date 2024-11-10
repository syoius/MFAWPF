using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MFAWPF.Core.Extensions;
using MFAWPF.Core.Services;

namespace MFAWPF.Avalonia.Views;

public partial class CropImageDialog : Window
{
    private Point _startPoint;
    private Rectangle? _selectionRectangle;
    private double _scaleRatio;
    private double _originWidth;
    private double _originHeight;

    public string? Output { get; set; }
    private List<int>? _outputRoi;

    public List<int>? OutputRoi
    {
        get => _outputRoi;
        set => _outputRoi = value?.Select(i => i < 0 ? 0 : i).ToList();
    }

    public CropImageDialog()
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

        _originWidth = imageSource.PixelSize.Width;
        _originHeight = imageSource.PixelSize.Height;

        double maxWidth = imageControl.MaxWidth ?? 1280;
        double maxHeight = imageControl.MaxHeight ?? 720;

        double widthRatio = maxWidth / _originWidth;
        double heightRatio = maxHeight / _originHeight;
        _scaleRatio = Math.Min(widthRatio, heightRatio);

        imageControl.Width = _originWidth * _scaleRatio;
        imageControl.Height = _originHeight * _scaleRatio;

        var canvas = this.FindControl<Canvas>("SelectionCanvas")!;
        canvas.Width = imageControl.Width;
        canvas.Height = imageControl.Height;
        Width = imageControl.Width + 20;
        Height = imageControl.Height + 100;

        Position = new PixelPoint(
            (int)((Screens.Primary.Bounds.Width - Width) / 2),
            (int)((Screens.Primary.Bounds.Height - Height) / 2));
    }

    private void Canvas_MouseDown(object? sender, PointerPressedEventArgs e)
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

    private void Canvas_MouseMove(object? sender, PointerEventArgs e)
    {
        var imageControl = this.FindControl<Image>("image")!;
        var position = e.GetPosition(imageControl);
        var textBlock = this.FindControl<TextBlock>("MousePositionText")!;
        textBlock.Text = $"[ {(int)(position.X / _scaleRatio)}, {(int)(position.Y / _scaleRatio)} ]";

        if (_selectionRectangle == null || !e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            return;

        var canvas = this.FindControl<Canvas>("SelectionCanvas")!;
        var pos = e.GetPosition(canvas);

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

        if (x + w > canvas.Bounds.Width)
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

    private void Canvas_MouseUp(object? sender, PointerReleasedEventArgs e)
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

        var x = Canvas.GetLeft(_selectionRectangle) / _scaleRatio;
        var y = Canvas.GetTop(_selectionRectangle) / _scaleRatio;
        var w = _selectionRectangle.Width / _scaleRatio;
        var h = _selectionRectangle.Height / _scaleRatio;

        SaveCroppedImage((int)x, (int)y, (int)w, (int)h);
    }

    private async void SaveCroppedImage(int x, int y, int width, int height)
    {
        var imageControl = this.FindControl<Image>("image")!;
        if (imageControl.Source is Bitmap bitmap)
        {
            var roiX = Math.Max(x - 5, 0);
            var roiY = Math.Max(y - 5, 0);
            var roiW = Math.Min(width + 10, bitmap.PixelSize.Width - roiX);
            var roiH = Math.Min(height + 10, bitmap.PixelSize.Height - roiY);
            OutputRoi = [roiX, roiY, roiW, roiH];

            using var croppedBitmap = new CroppedBitmap(bitmap, 
                new PixelRect(x, y, width, height));

            var dialog = new SaveFileDialog
            {
                Title = "SaveFileTitle".GetLocalizationString(),
                DefaultExtension = "png",
                Filters = new List<FileDialogFilter>
                {
                    new() { Name = "ImageFilter".GetLocalizationString(), Extensions = new List<string> { "png" } }
                }
            };

            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                using var fs = File.OpenWrite(result);
                croppedBitmap.Save(fs);
                Output = Path.GetFileName(result);
                Close(true);
            }
        }
    }
}