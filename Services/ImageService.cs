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
    private readonly Settings _settings;

    // 次の1枚の先読み。
    // 先読みタスクはワーカースレッドから書き込むため、必ず _preloadLock 下で扱う。
    // _preloaded は「_preloadedPath の画像」であることを保証する。この対応付けが無いと、
    // 古い先読みタスクが完了時に新しい値を上書きし、直前に返した画像がもう一度返ってしまう。
    private readonly object _preloadLock = new();
    private string? _preloadedPath;    // いま先読み対象にしているパス
    private BitmapImage? _preloaded;   // ロードが終わっていればここに入る
    private Task? _preloadTask;

    public ImageService(Settings settings)
    {
        _settings = settings;
        Reload();
    }

    public void Reload()
    {
        _files = [];
        _index = 0;
        ClearPreload();

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

        string path = _files[_index];

        // 先読み済みが「いま欲しい1枚」と一致するときだけ使う。
        // 一致しないものは古いタスクの結果なので捨てる（重複表示の原因になる）。
        BitmapImage? img;
        lock (_preloadLock)
        {
            img = _preloadedPath == path ? _preloaded : null;
            _preloaded     = null;
            _preloadedPath = null;
        }
        // 先読みが間に合っていなければ同期ロードにフォールバックする
        img ??= LoadImage(path);

        _index = (_index + 1) % _files.Count;
        if (_index == 0 && _settings.Shuffle)
            Shuffle(_files, avoidFirst: path);

        StartPreload(_files[_index]);

        return img;
    }

    /// <summary>次の1枚をバックグラウンドで先読みする。</summary>
    private void StartPreload(string path)
    {
        lock (_preloadLock)
        {
            // 期待するパスを先に確定させておく。
            // これより後に完了した古いタスクは、ここと一致せず破棄される。
            _preloadedPath = path;
            _preloaded     = null;
        }

        _preloadTask = Task.Run(() =>
        {
            var bmp = LoadImage(path);
            lock (_preloadLock)
            {
                // ロード中に次の要求が来ていたら、この結果はもう古い
                if (_preloadedPath == path)
                    _preloaded = bmp;
            }
        });
    }

    private void ClearPreload()
    {
        lock (_preloadLock)
        {
            _preloaded     = null;
            _preloadedPath = null;   // 実行中のタスクの書き込みも破棄させる
        }
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

    /// <param name="avoidFirst">
    /// シャッフル後の先頭に来てほしくないパス（＝直前に表示した1枚）。
    /// 一周して再シャッフルするとき、新しい先頭が直前の1枚と同じだと
    /// 周回の境目で同じ画像が連続してしまうため、指定があれば入れ替える。
    /// </param>
    private void Shuffle(List<string> list, string? avoidFirst = null)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        if (avoidFirst != null && list.Count > 1 && list[0] == avoidFirst)
        {
            int k = 1 + _random.Next(list.Count - 1);
            (list[0], list[k]) = (list[k], list[0]);
        }
    }

    public int Count => _files.Count;

    public void Dispose()
    {
        // 実行中の先読みタスクが完了しても、破棄済みの画像を書き戻さないようにする
        ClearPreload();
    }
}
