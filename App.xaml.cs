using System.Windows;
using MyPhotoScreensaver.Services;
using MyPhotoScreensaver.Windows;
using WpfApplication = System.Windows.Application;

namespace MyPhotoScreensaver;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var args = e.Args;
        var settings = SettingsService.Load();

        if (args.Length == 0)
        {
            // No args: show settings
            ShowSettings();
            return;
        }

        var arg = args[0].ToLowerInvariant().TrimStart('/').TrimStart('-');

        if (arg == "s")
        {
            // Full-screen screensaver
            ScreensaverManager.Launch(settings);
        }
        else if (arg.StartsWith("c"))
        {
            // Settings dialog (/c or /c:HWND)
            ShowSettings();
        }
        else if (arg == "p" && args.Length >= 2)
        {
            // Preview in control panel mini window
            if (long.TryParse(args[1], out long hwndLong))
            {
                var handle = new IntPtr(hwndLong);
                var win = new ScreensaverWindow(settings, isPreview: true, previewHandle: handle);
                win.Show();
            }
            else
            {
                ShowSettings();
            }
        }
        else
        {
            ShowSettings();
        }
    }

    private static void ShowSettings()
    {
        var dlg = new SettingsWindow();
        dlg.ShowDialog();
        WpfApplication.Current.Shutdown();
    }
}
