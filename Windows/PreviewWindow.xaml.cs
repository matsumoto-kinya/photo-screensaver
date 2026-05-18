using System.Windows;
using System.Windows.Threading;
using MyPhotoScreensaver.Models;
using MyPhotoScreensaver.Services;
using MyPhotoScreensaver.Transitions;

namespace MyPhotoScreensaver.Windows;

public partial class PreviewWindow : Window
{
    private readonly Settings _settings;
    private readonly ImageService _imageService;
    private readonly TransitionEngine _transition = new();
    private readonly DispatcherTimer _timer;
    private bool _front = false;
    private bool _transitioning = false;
    private PanelController? _panelController;

    public PreviewWindow(Settings settings)
    {
        InitializeComponent();
        _settings = settings;
        _imageService = new ImageService(settings);
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Min(settings.DisplaySeconds, 5))
        };
        _timer.Tick += async (_, _) => await ShowNextAsync();
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            _timer.Stop();
            _panelController?.Dispose();
            _imageService.Dispose();
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_imageService.Count == 0)
        {
            NoImagesText.Visibility = Visibility.Visible;
            return;
        }

        if (_settings.Transition == TransitionType.Panel)
        {
            await InitPanelModeAsync();
        }
        else
        {
            var bitmap = _imageService.GetNext();
            if (bitmap != null)
            {
                ImageA.Source   = bitmap;
                ImageA.Stretch  = _settings.FitMode == FitMode.Crop
                    ? System.Windows.Media.Stretch.UniformToFill
                    : System.Windows.Media.Stretch.Uniform;
                ImageA.Visibility = Visibility.Visible;
            }
            _timer.Start();
        }
    }

    private async Task InitPanelModeAsync()
    {
        ImageA.Visibility = Visibility.Collapsed;
        ImageB.Visibility = Visibility.Collapsed;
        PanelCanvas.Visibility = Visibility.Visible;

        await Task.Delay(50);

        double w = ActualWidth  > 10 ? ActualWidth  : 640;
        double h = ActualHeight > 10 ? ActualHeight : 400;
        PanelCanvas.Width  = w;
        PanelCanvas.Height = h;

        _panelController = new PanelController(PanelCanvas, w, h, _settings, _imageService);
        await _panelController.StartAsync();
    }

    private async Task ShowNextAsync()
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
}
