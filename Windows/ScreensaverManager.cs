using System.Windows;
using System.Windows.Forms;
using MyPhotoScreensaver.Models;

namespace MyPhotoScreensaver.Windows;

/// <summary>
/// Manages one ScreensaverWindow per monitor.
/// </summary>
public static class ScreensaverManager
{
    public static void Launch(Settings settings)
    {
        var screens = Screen.AllScreens;
        var windows = new List<ScreensaverWindow>();

        foreach (var screen in screens)
        {
            var win = new ScreensaverWindow(settings);
            win.Left = screen.Bounds.Left;
            win.Top = screen.Bounds.Top;
            win.Width = screen.Bounds.Width;
            win.Height = screen.Bounds.Height;
            win.WindowState = WindowState.Normal;
            win.WindowStyle = WindowStyle.None;
            win.ResizeMode = ResizeMode.NoResize;
            windows.Add(win);
        }

        foreach (var win in windows)
            win.Show();
    }
}
