using System.IO;
using System.Windows.Media.Imaging;
using MyPhotoScreensaver.Models;

namespace MyPhotoScreensaver.Services;

public class ImageService : IDisposable
{
    private static readonly string[] Extensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];

    private List<string> _files = [];
    private int _index = 0;
    private readonly Random _random = new();
    private BitmapImage? _preloaded;
    private Task? _preloadTask;
    private readonly Settings _settings;

    public ImageService(Settings settings)
    {
        _settings = settings;
        Reload();
    }

    public void Reload()
    {
        _files = [];
        _index = 0;

        if (!Directory.Exists(_settings.ImageFolder))
            return;

        var files = Directory.EnumerateFiles(_settings.ImageFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        if (_settings.Shuffle)
            Shuffle(files);

        _files = files;
    }

    public BitmapImage? GetNext()
    {
        if (_files.Count == 0) return null;

        // Use preloaded image if ready
        BitmapImage? img = null;
        if (_preloaded != null)
        {
            img = _preloaded;
            _preloaded = null;
        }
        else
        {
            img = LoadImage(_files[_index]);
        }

        _index = (_index + 1) % _files.Count;
        if (_index == 0 && _settings.Shuffle)
            Shuffle(_files);

        // Start preloading next
        var nextPath = _files[_index];
        _preloadTask = Task.Run(() =>
        {
            _preloaded = LoadImage(nextPath);
        });

        return img;
    }

    private static BitmapImage? LoadImage(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 1920; // limit memory
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public int Count => _files.Count;

    public void Dispose()
    {
        _preloaded = null;
    }
}
