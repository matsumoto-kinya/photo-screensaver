using System.Windows;
using System.Windows.Controls;
using MyPhotoScreensaver.Models;
using MyPhotoScreensaver.Services;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace MyPhotoScreensaver.Windows;

public partial class SettingsWindow : Window
{
    private Settings _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Load();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        FolderTextBox.Text = _settings.ImageFolder;
        DisplaySecondsSlider.Value = _settings.DisplaySeconds;
        TransitionSpeedSlider.Value = _settings.TransitionSpeed;
        ShuffleCheckBox.IsChecked = _settings.Shuffle;

        SelectComboItem(TransitionComboBox, _settings.Transition.ToString());
        SelectComboItem(FitModeComboBox, _settings.FitMode.ToString());
    }

    private static void SelectComboItem(WpfComboBox combo, string tag)
    {
        foreach (WpfComboBoxItem item in combo.Items)
        {
            if (item.Tag?.ToString() == tag)
            {
                combo.SelectedItem = item;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "画像フォルダを選択してください",
            SelectedPath = FolderTextBox.Text
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            FolderTextBox.Text = dialog.SelectedPath;
    }

    private void DisplaySecondsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DisplaySecondsLabel != null)
            DisplaySecondsLabel.Text = ((int)e.NewValue).ToString();
    }

    private void TransitionSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TransitionSpeedLabel != null)
            TransitionSpeedLabel.Text = e.NewValue.ToString("F1");
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        ApplyToSettings();
        var preview = new PreviewWindow(_settings);
        preview.Owner = this;
        preview.ShowDialog();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        ApplyToSettings();
        SettingsService.Save(_settings);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ApplyToSettings()
    {
        _settings.ImageFolder = FolderTextBox.Text;
        _settings.DisplaySeconds = (int)DisplaySecondsSlider.Value;
        _settings.TransitionSpeed = Math.Round(TransitionSpeedSlider.Value, 1);
        _settings.Shuffle = ShuffleCheckBox.IsChecked == true;

        if (TransitionComboBox.SelectedItem is WpfComboBoxItem ti && ti.Tag is string tt)
            _settings.Transition = Enum.Parse<TransitionType>(tt);

        if (FitModeComboBox.SelectedItem is WpfComboBoxItem fi && fi.Tag is string ft)
            _settings.FitMode = Enum.Parse<FitMode>(ft);
    }
}
