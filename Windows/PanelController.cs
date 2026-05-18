using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MyPhotoScreensaver.Models;
using MyPhotoScreensaver.Services;
using WpfImage = System.Windows.Controls.Image;

namespace MyPhotoScreensaver.Windows;

/// <summary>
/// 3行・可変列のパネルグリッドを管理する。
/// 各セルの横幅は画像のアスペクト比から計算した自然幅を行全体でジャスティファイして決定し、
/// 画像の入れ替えのたびに再計算・リサイズアニメーションを行う。
/// </summary>
public class PanelController : IDisposable
{
    private const int Rows    = 4;
    private const int MinCols = 2;
    private const int MaxCols = 10;

    private readonly Canvas          _canvas;
    private readonly double          _width;   // キャンバス幅
    private readonly double          _cellH;   // = canvasHeight / Rows (固定)
    private readonly Settings        _settings;
    private readonly ImageService    _imageService;
    private readonly DispatcherTimer _timer;
    private readonly Random          _rng  = new();
    private readonly IEasingFunction _ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
    private bool _animating;

    // StartAsync で確定する
    private int         _cols;
    private WpfImage[,] _images        = null!;
    private double[,]   _naturalWidths = null!; // [col, row]: cellH × imageAR

    public PanelController(Canvas canvas, double width, double height,
        Settings settings, ImageService imageService)
    {
        _canvas       = canvas;
        _width        = width;
        _cellH        = height / Rows;
        _settings     = settings;
        _imageService = imageService;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(settings.DisplaySeconds)
        };
        _timer.Tick += async (_, _) => await RunReplacementAsync();
    }

    // ── ヘルパー ──────────────────────────────────────────────────────────

    /// 画像の自然幅 = cellH × (imageWidth / imageHeight)
    private double NaturalWidth(BitmapImage? bmp)
        => bmp?.PixelHeight > 0 ? _cellH * bmp.PixelWidth / bmp.PixelHeight : _cellH;

    /// 行の自然幅合計が _width になるようスケールした表示幅を返す
    private double[] RowDisplayWidths(int row)
    {
        double total = 0;
        for (int c = 0; c < _cols; c++) total += _naturalWidths[c, row];
        double scale = total > 0 ? _width / total : 1.0;
        var w = new double[_cols];
        for (int c = 0; c < _cols; c++) w[c] = _naturalWidths[c, row] * scale;
        return w;
    }

    private static double[] XPositions(double[] widths)
    {
        var xs = new double[widths.Length];
        double x = 0;
        for (int i = 0; i < widths.Length; i++) { xs[i] = x; x += widths[i]; }
        return xs;
    }

    // ── 起動 ──────────────────────────────────────────────────────────────

    public async Task StartAsync()
    {
        if (_imageService.Count == 0) return;

        // 平均 AR 算出のためにサンプル画像をロード
        int sampleCount = Math.Min(Rows * MaxCols, _imageService.Count);
        var samples = new List<BitmapImage>(sampleCount);
        for (int i = 0; i < sampleCount; i++)
        {
            var bmp = await Task.Run(() => _imageService.GetNext());
            if (bmp != null) samples.Add(bmp);
        }
        if (samples.Count == 0) return;

        double avgAR = samples.Average(b =>
            b.PixelWidth > 0 && b.PixelHeight > 0
                ? (double)b.PixelWidth / b.PixelHeight : 4.0 / 3.0);

        _cols = Math.Max(MinCols, Math.Min(MaxCols,
                    (int)Math.Round(_width / (_cellH * avgAR))));

        _images        = new WpfImage[_cols, Rows];
        _naturalWidths = new double[_cols, Rows];

        // Image コントロールを生成
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < _cols; c++)
            {
                var img = new WpfImage { Stretch = Stretch.UniformToFill };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                _canvas.Children.Add(img);
                _images[c, r] = img;
            }

        // 画像を割り当て → 各行のレイアウト確定
        int idx = 0;
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                BitmapImage? bmp = idx < samples.Count
                    ? samples[idx++]
                    : await Task.Run(() => _imageService.GetNext());
                _images[c, r].Source  = bmp;
                _naturalWidths[c, r]  = NaturalWidth(bmp);
            }
            ApplyRowLayout(r);
        }

        _timer.Start();
    }

    /// 自然幅からジャスティファイした表示幅・座標を Image コントロールに即時反映
    private void ApplyRowLayout(int row)
    {
        var widths = RowDisplayWidths(row);
        var xs     = XPositions(widths);
        for (int c = 0; c < _cols; c++)
        {
            _images[c, row].Width  = widths[c];
            _images[c, row].Height = _cellH;
            Canvas.SetLeft(_images[c, row], xs[c]);
            Canvas.SetTop(_images[c, row],  row * _cellH);
        }
    }

    // ── 入れ替えオーケストレーション ─────────────────────────────────────

    private async Task RunReplacementAsync()
    {
        if (_animating) return;
        _animating = true;
        _timer.Stop();
        try
        {
            // 可変幅セルは行ごとに独立したレイアウトなので行モードのみ
            await ReplaceRowAsync(_rng.Next(Rows), _rng.Next(2) == 0);
        }
        finally
        {
            _animating = false;
            _timer.Start();
        }
    }

    /// <param name="goRight">
    /// true  = 右端が退場・左から入場（他セルは右へシフト）
    /// false = 左端が退場・右から入場（他セルは左へシフト）
    /// </param>
    private async Task ReplaceRowAsync(int row, bool goRight)
    {
        double d     = _settings.TransitionSpeed;
        int exitCol  = goRight ? _cols - 1 : 0;
        int entryCol = goRight ? 0 : _cols - 1;

        var curWidths = RowDisplayWidths(row);
        WpfImage exitImg = _images[exitCol, row];

        // ── Step 1: 対象セルが画面外へスライドアウト ──────────────────────
        double exitDestX = goRight ? _width : -curWidths[exitCol];
        await MoveAsync(exitImg, exitDestX, row * _cellH, d * 0.3);

        // ── 新画像ロード & 新レイアウト計算 ───────────────────────────────
        var newBmp  = await Task.Run(() => _imageService.GetNext());
        double newNW = NaturalWidth(newBmp);

        // 新しい自然幅配列（コンベア方向に1つシフト + 新画像を端に挿入）
        var newNaturals = new double[_cols];
        if (goRight)
        {
            newNaturals[0] = newNW;
            for (int c = 1; c < _cols; c++)
                newNaturals[c] = _naturalWidths[c - 1, row];
        }
        else
        {
            newNaturals[_cols - 1] = newNW;
            for (int c = 0; c < _cols - 1; c++)
                newNaturals[c] = _naturalWidths[c + 1, row];
        }

        double totalNew = newNaturals.Sum();
        double scaleNew = totalNew > 0 ? _width / totalNew : 1.0;
        var newWidths   = newNaturals.Select(n => n * scaleNew).ToArray();
        var newXs       = XPositions(newWidths);

        // 入場セル（退場コントロールを再利用）の初期位置をセット
        exitImg.Source = newBmp;
        exitImg.Width  = newWidths[entryCol];
        exitImg.Height = _cellH;
        Canvas.SetLeft(exitImg, goRight ? -newWidths[entryCol] : _width);
        Canvas.SetTop(exitImg,  row * _cellH);

        // ── Step 2+3 同時: 残セルがリサイズ+移動、新セルがスライドイン ──
        var tasks = new List<Task>();

        if (goRight)
        {
            // 元の col c (0..N-2) → 新 col c+1 へリサイズしながら移動
            for (int c = 0; c < _cols - 1; c++)
                tasks.Add(ResizeMoveAsync(_images[c, row],
                    newXs[c + 1], row * _cellH, newWidths[c + 1], _cellH, d * 0.7));
        }
        else
        {
            // 元の col c (1..N-1) → 新 col c-1 へリサイズしながら移動
            for (int c = 1; c < _cols; c++)
                tasks.Add(ResizeMoveAsync(_images[c, row],
                    newXs[c - 1], row * _cellH, newWidths[c - 1], _cellH, d * 0.7));
        }
        // 新セルのスライドイン（幅は固定、位置のみアニメーション）
        tasks.Add(MoveAsync(exitImg, newXs[entryCol], row * _cellH, d * 0.7));

        await Task.WhenAll(tasks);

        // ── グリッド状態を更新 ────────────────────────────────────────────
        var newImages = new WpfImage[_cols];
        if (goRight)
        {
            newImages[0] = exitImg;
            for (int c = 1; c < _cols; c++) newImages[c] = _images[c - 1, row];
        }
        else
        {
            newImages[_cols - 1] = exitImg;
            for (int c = 0; c < _cols - 1; c++) newImages[c] = _images[c + 1, row];
        }
        for (int c = 0; c < _cols; c++)
        {
            _images[c, row]        = newImages[c];
            _naturalWidths[c, row] = newNaturals[c];
        }
    }

    // ── アニメーションヘルパー ────────────────────────────────────────────

    /// 位置のみ移動（幅・高さ変更なし）
    private Task MoveAsync(WpfImage img, double toX, double toY, double durationSec)
        => ResizeMoveAsync(img, toX, toY,
            double.IsNaN(img.Width)  ? 0 : img.Width,
            double.IsNaN(img.Height) ? 0 : img.Height,
            durationSec);

    /// 位置と幅・高さを同時にアニメーション
    private Task ResizeMoveAsync(WpfImage img,
        double toX, double toY, double toW, double toH, double durationSec)
    {
        var tcs  = new TaskCompletionSource();
        double fromX = Canvas.GetLeft(img);
        double fromY = Canvas.GetTop(img);
        double fromW = double.IsNaN(img.Width)  ? toW : img.Width;
        double fromH = double.IsNaN(img.Height) ? toH : img.Height;

        bool aX = Math.Abs(fromX - toX) > 0.5;
        bool aY = Math.Abs(fromY - toY) > 0.5;
        bool aW = Math.Abs(fromW - toW) > 0.5;
        bool aH = Math.Abs(fromH - toH) > 0.5;

        if (!aX && !aY && !aW && !aH)
        {
            Canvas.SetLeft(img, toX); Canvas.SetTop(img, toY);
            img.Width = toW; img.Height = toH;
            tcs.TrySetResult();
            return tcs.Task;
        }

        var sb  = new Storyboard();
        var dur = TimeSpan.FromSeconds(durationSec);

        void Anim(double from, double to, PropertyPath path)
        {
            if (Math.Abs(from - to) <= 0.5) return;
            var a = new DoubleAnimation(from, to, dur) { EasingFunction = _ease };
            Storyboard.SetTarget(a, img);
            Storyboard.SetTargetProperty(a, path);
            sb.Children.Add(a);
        }

        Anim(fromX, toX, new PropertyPath("(Canvas.Left)"));
        Anim(fromY, toY, new PropertyPath("(Canvas.Top)"));
        Anim(fromW, toW, new PropertyPath(FrameworkElement.WidthProperty));
        Anim(fromH, toH, new PropertyPath(FrameworkElement.HeightProperty));

        sb.Completed += (_, _) =>
        {
            Canvas.SetLeft(img, toX); Canvas.SetTop(img, toY);
            img.Width = toW; img.Height = toH;
            sb.Stop();
            tcs.TrySetResult();
        };
        sb.Begin();
        return tcs.Task;
    }

    public void Dispose() => _timer.Stop();
}
