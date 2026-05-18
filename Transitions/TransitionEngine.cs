using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MyPhotoScreensaver.Models;
using WpfImage = System.Windows.Controls.Image;

namespace MyPhotoScreensaver.Transitions;

public class TransitionEngine
{
    private readonly Random _random = new();

    /// <summary>
    /// Runs transition: hides oldImage, shows newImage with effect, returns when complete.
    /// Both images are direct children of a Grid (Canvas overlay for dissolve).
    /// </summary>
    public Task RunAsync(WpfImage oldImage, WpfImage newImage, BitmapImage bitmap,
        TransitionType type, double durationSec, FitMode fitMode)
    {
        newImage.Source = bitmap;
        newImage.Stretch = fitMode == FitMode.Crop ? Stretch.UniformToFill : Stretch.Uniform;
        oldImage.Stretch = fitMode == FitMode.Crop ? Stretch.UniformToFill : Stretch.Uniform;

        var effectiveType = type == TransitionType.Random ? RandomType() : type;

        return effectiveType switch
        {
            TransitionType.Fade => FadeTransition(oldImage, newImage, durationSec),
            TransitionType.SlideLeft => SlideTransition(oldImage, newImage, durationSec, -1, 0),
            TransitionType.SlideRight => SlideTransition(oldImage, newImage, durationSec, 1, 0),
            TransitionType.SlideUp => SlideTransition(oldImage, newImage, durationSec, 0, -1),
            TransitionType.SlideDown => SlideTransition(oldImage, newImage, durationSec, 0, 1),
            TransitionType.ZoomIn => ZoomTransition(oldImage, newImage, durationSec),
            TransitionType.Dissolve => DissolveTransition(oldImage, newImage, durationSec),
            TransitionType.Panel => FadeTransition(oldImage, newImage, durationSec), // handled by PanelController
            _ => FadeTransition(oldImage, newImage, durationSec)
        };
    }

    private TransitionType RandomType()
    {
        var types = new[]
        {
            TransitionType.Fade,
            TransitionType.SlideLeft, TransitionType.SlideRight,
            TransitionType.SlideUp, TransitionType.SlideDown,
            TransitionType.ZoomIn, TransitionType.Dissolve
            // Panel は独自モードのため Random には含めない
        };
        return types[_random.Next(types.Length)];
    }

    private static Task FadeTransition(WpfImage oldImage, WpfImage newImage, double duration)
    {
        var tcs = new TaskCompletionSource();
        var dur = TimeSpan.FromSeconds(duration);

        newImage.Opacity = 0;
        newImage.Visibility = Visibility.Visible;

        var sb = new Storyboard();

        var fadeIn = new DoubleAnimation(0, 1, dur) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        Storyboard.SetTarget(fadeIn, newImage);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));

        var fadeOut = new DoubleAnimation(1, 0, dur) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        Storyboard.SetTarget(fadeOut, oldImage);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));

        sb.Children.Add(fadeIn);
        sb.Children.Add(fadeOut);
        sb.Completed += (_, _) =>
        {
            oldImage.Visibility = Visibility.Hidden;
            oldImage.Opacity = 1;
            tcs.TrySetResult();
        };
        sb.Begin();
        return tcs.Task;
    }

    private static Task SlideTransition(WpfImage oldImage, WpfImage newImage, double duration, int dx, int dy)
    {
        var tcs = new TaskCompletionSource();
        var dur = TimeSpan.FromSeconds(duration);
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

        double w = newImage.ActualWidth > 0 ? newImage.ActualWidth : SystemParameters.PrimaryScreenWidth;
        double h = newImage.ActualHeight > 0 ? newImage.ActualHeight : SystemParameters.PrimaryScreenHeight;

        var newTransform = new TranslateTransform(dx * w, dy * h);
        newImage.RenderTransform = newTransform;
        newImage.Visibility = Visibility.Visible;
        newImage.Opacity = 1;

        var oldTransform = new TranslateTransform(0, 0);
        oldImage.RenderTransform = oldTransform;

        var sb = new Storyboard();

        void AddAnim(TranslateTransform t, string prop, double from, double to, UIElement target)
        {
            var anim = new DoubleAnimation(from, to, dur) { EasingFunction = ease };
            Storyboard.SetTarget(anim, target);
            Storyboard.SetTargetProperty(anim, new PropertyPath($"RenderTransform.{prop}"));
            sb.Children.Add(anim);
        }

        if (dx != 0)
        {
            AddAnim(newTransform, "X", dx * w, 0, newImage);
            AddAnim(oldTransform, "X", 0, -dx * w, oldImage);
        }
        else
        {
            AddAnim(newTransform, "Y", dy * h, 0, newImage);
            AddAnim(oldTransform, "Y", 0, -dy * h, oldImage);
        }

        sb.Completed += (_, _) =>
        {
            oldImage.Visibility = Visibility.Hidden;
            oldImage.RenderTransform = null;
            newImage.RenderTransform = null;
            tcs.TrySetResult();
        };
        sb.Begin();
        return tcs.Task;
    }

    private static Task ZoomTransition(WpfImage oldImage, WpfImage newImage, double duration)
    {
        var tcs = new TaskCompletionSource();
        var dur = TimeSpan.FromSeconds(duration);
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

        newImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        var scale = new ScaleTransform(1.3, 1.3);
        newImage.RenderTransform = scale;
        newImage.Opacity = 0;
        newImage.Visibility = Visibility.Visible;

        var sb = new Storyboard();

        var scaleX = new DoubleAnimation(1.3, 1.0, dur) { EasingFunction = ease };
        Storyboard.SetTarget(scaleX, newImage);
        Storyboard.SetTargetProperty(scaleX, new PropertyPath("RenderTransform.ScaleX"));

        var scaleY = new DoubleAnimation(1.3, 1.0, dur) { EasingFunction = ease };
        Storyboard.SetTarget(scaleY, newImage);
        Storyboard.SetTargetProperty(scaleY, new PropertyPath("RenderTransform.ScaleY"));

        var fadeIn = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        Storyboard.SetTarget(fadeIn, newImage);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(duration * 0.5)) { EasingFunction = ease };
        Storyboard.SetTarget(fadeOut, oldImage);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));

        sb.Children.Add(scaleX);
        sb.Children.Add(scaleY);
        sb.Children.Add(fadeIn);
        sb.Children.Add(fadeOut);

        sb.Completed += (_, _) =>
        {
            oldImage.Visibility = Visibility.Hidden;
            oldImage.Opacity = 1;
            newImage.RenderTransform = null;
            tcs.TrySetResult();
        };
        sb.Begin();
        return tcs.Task;
    }

    private static Task DissolveTransition(WpfImage oldImage, WpfImage newImage, double duration)
    {
        // Dissolve: Use opacity animation with a noise-like approach via OpacityMask
        // For simplicity, do a stepped fade to simulate dissolve
        var tcs = new TaskCompletionSource();
        var dur = TimeSpan.FromSeconds(duration);

        newImage.Opacity = 0;
        newImage.Visibility = Visibility.Visible;

        int steps = 20;
        double stepDuration = duration / steps;
        int step = 0;

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(stepDuration)
        };

        timer.Tick += (_, _) =>
        {
            step++;
            double progress = (double)step / steps;
            newImage.Opacity = progress;
            oldImage.Opacity = 1 - progress;

            if (step >= steps)
            {
                timer.Stop();
                oldImage.Visibility = Visibility.Hidden;
                oldImage.Opacity = 1;
                newImage.Opacity = 1;
                tcs.TrySetResult();
            }
        };
        timer.Start();
        return tcs.Task;
    }

    private Task PanelTransition(WpfImage oldImage, WpfImage newImage, BitmapImage bitmap,
        double duration, FitMode fitMode)
    {
        var tcs = new TaskCompletionSource();
        var parent = oldImage.Parent as System.Windows.Controls.Panel;
        if (parent == null) { tcs.TrySetResult(); return tcs.Task; }

        double width  = parent.ActualWidth;
        double height = parent.ActualHeight;

        const int cols = 6;
        const int rows = 4;
        double tileW = Math.Ceiling(width  / cols);
        double tileH = Math.Ceiling(height / rows);

        // Overlay canvas placed on top of both images
        var overlay = new System.Windows.Controls.Canvas
        {
            Width = width, Height = height,
            ClipToBounds = true,
            IsHitTestVisible = false
        };
        System.Windows.Controls.Panel.SetZIndex(overlay, 10);
        parent.Children.Add(overlay);

        // Choose a random slide direction for all tiles this transition
        // 0=right, 1=left, 2=down, 3=up
        int dir = _random.Next(4);

        double tileDuration = duration * 0.70;
        double maxDelay     = duration * 0.30;

        var storyboard = new Storyboard();
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                double finalX = col * tileW;
                double finalY = row * tileH;

                // Stagger tiles in the direction of travel (wave effect)
                double wave = dir switch
                {
                    0 => (double)col / (cols - 1),
                    1 => (double)(cols - 1 - col) / (cols - 1),
                    2 => (double)row / (rows - 1),
                    3 => (double)(rows - 1 - row) / (rows - 1),
                    _ => 0
                };
                double delay = wave * maxDelay;

                double startX = finalX, startY = finalY;
                switch (dir)
                {
                    case 0: startX = -tileW;  break;  // slide in from left
                    case 1: startX =  width;  break;  // slide in from right
                    case 2: startY = -tileH;  break;  // slide in from top
                    case 3: startY =  height; break;  // slide in from bottom
                }

                // Each tile shows its exact portion of the new image
                var brush = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.Fill,
                    ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
                    Viewbox = new Rect(
                        (double)col / cols,
                        (double)row / rows,
                        1.0 / cols,
                        1.0 / rows)
                };

                var tile = new System.Windows.Controls.Border
                {
                    Width      = tileW + 1,  // +1 to prevent hairline gaps
                    Height     = tileH + 1,
                    Background = brush
                };
                System.Windows.Controls.Canvas.SetLeft(tile, startX);
                System.Windows.Controls.Canvas.SetTop(tile,  startY);
                overlay.Children.Add(tile);

                bool horizontal = dir <= 1;
                var anim = new DoubleAnimation(
                    horizontal ? startX : startY,
                    horizontal ? finalX : finalY,
                    TimeSpan.FromSeconds(tileDuration))
                {
                    BeginTime       = TimeSpan.FromSeconds(delay),
                    EasingFunction  = ease
                };
                Storyboard.SetTarget(anim, tile);
                Storyboard.SetTargetProperty(anim, new PropertyPath(
                    horizontal ? "(Canvas.Left)" : "(Canvas.Top)"));
                storyboard.Children.Add(anim);
            }
        }

        storyboard.Completed += (_, _) =>
        {
            parent.Children.Remove(overlay);
            newImage.Source     = bitmap;
            newImage.Stretch    = fitMode == FitMode.Crop ? Stretch.UniformToFill : Stretch.Uniform;
            newImage.Opacity    = 1;
            newImage.Visibility = Visibility.Visible;
            oldImage.Visibility = Visibility.Hidden;
            tcs.TrySetResult();
        };
        storyboard.Begin();
        return tcs.Task;
    }
}
