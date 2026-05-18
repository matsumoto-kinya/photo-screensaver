using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MyPhotoScreensaver.Models;
using MyPhotoScreensaver.Services;
using MyPhotoScreensaver.Transitions;
using WpfPoint = System.Windows.Point;

namespace MyPhotoScreensaver.Windows;

public partial class ScreensaverWindow : Window
{
    private readonly Settings _settings;
    private readonly ImageService _imageService;
    private readonly TransitionEngine _transition = new();
    private readonly DispatcherTimer _timer;
    private bool _front = false;
    private bool _transitioning = false;
    private WpfPoint _lastMousePos;
    private bool _mouseInitialized = false;
    private bool _isPreview;
    private IntPtr _previewHandle;
    private PanelController? _panelController;

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const int GWL_STYLE = -16;
    private const int WS_CHILD = 0x40000000;

    public ScreensaverWindow(Settings settings, bool isPreview = false, IntPtr previewHandle = default)
    {
        InitializeComponent();
        _settings = settings;
        _isPreview = isPreview;
        _previewHandle = previewHandle;
        _imageService = new ImageService(settings);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(settings.DisplaySeconds)
        };
        _timer.Tick += async (_, _) => await ShowNextImageAsync();

        Loaded += OnLoaded;
        KeyDown += (_, _) => ExitScreensaver();
        MouseDown += (_, _) => ExitScreensaver();
        MouseMove += OnMouseMove;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isPreview && _previewHandle != IntPtr.Zero)
            SetupPreviewMode();
        else
            WindowState = WindowState.Maximized;

        if (_settings.Transition == TransitionType.Panel)
        {
            await InitPanelModeAsync();
        }
        else
        {
            if (_imageService.Count > 0)
            {
                var bitmap = _imageService.GetNext();
                if (bitmap != null) ShowImmediate(bitmap);
            }
            _timer.Start();
        }
    }

    private async Task InitPanelModeAsync()
    {
        ImageA.Visibility = Visibility.Collapsed;
        ImageB.Visibility = Visibility.Collapsed;
        PanelCanvas.Visibility = Visibility.Visible;

        // Wait for the window to reach its final size after Maximized or preview setup
        await Task.Delay(80);

        double w = ActualWidth  > 10 ? ActualWidth  : System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Width;
        double h = ActualHeight > 10 ? ActualHeight : System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Height;

        PanelCanvas.Width  = w;
        PanelCanvas.Height = h;

        _panelController = new PanelController(PanelCanvas, w, h, _settings, _imageService);
        await _panelController.StartAsync();
    }

    private void SetupPreviewMode()
    {
        var helper = new WindowInteropHelper(this);
        SetParent(helper.Handle, _previewHandle);
        SetWindowLong(helper.Handle, GWL_STYLE,
            GetWindowLong(helper.Handle, GWL_STYLE) | WS_CHILD);

        GetClientRect(_previewHandle, out var rect);
        Width  = rect.Right  - rect.Left;
        Height = rect.Bottom - rect.Top;
        Left = 0;
        Top  = 0;
        WindowState = WindowState.Normal;
        WindowStyle = WindowStyle.None;
    }

    private void ShowImmediate(BitmapImage bitmap)
    {
        var current = _front ? ImageB : ImageA;
        current.Source  = bitmap;
        current.Stretch = _settings.FitMode == FitMode.Crop
            ? System.Windows.Media.Stretch.UniformToFill
            : System.Windows.Media.Stretch.Uniform;
        current.Visibility = Visibility.Visible;
        current.Opacity    = 1;
    }

    private async Task ShowNextImageAsync()
    {
        if (_transitioning) return;
        var bitmap = await Task.Run(() => _imageService.GetNext());
        if (bitmap == null) return;

        _transitioning = true;
        _timer.Stop();

        var (oldImg, newImg) = _front ? (ImageB, ImageA) : (ImageA, ImageB);
        await _transition.RunAsync(oldImg, newImg, bitmap,
            _settings.Transition, _settings.TransitionSpeed, _settings.FitMode);

        _front = !_front;
        _transitioning = false;
        _timer.Start();
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (!_mouseInitialized)
        {
            _lastMousePos = pos;
            _mouseInitialized = true;
            return;
        }
        if (Math.Abs(pos.X - _lastMousePos.X) > 5 || Math.Abs(pos.Y - _lastMousePos.Y) > 5)
            ExitScreensaver();
    }

    private void ExitScreensaver()
    {
        if (_isPreview) return;
        _timer.Stop();
        _panelController?.Dispose();
        _imageService.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _panelController?.Dispose();
        _imageService.Dispose();
        base.OnClosed(e);
    }
}
