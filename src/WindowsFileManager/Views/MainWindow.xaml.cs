using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WindowsFileManager.ViewModels;

namespace WindowsFileManager.Views;

/// <summary>
/// Main window code-behind.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SaveSettings();
        }
    }

    private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MediaElement media)
        {
            return;
        }

        // Sync volume from sliders
        if (media == VideoPlayer)
        {
            media.Volume = VideoVolumeSlider.Value;
        }
        else if (media == AudioPlayer)
        {
            media.Volume = AudioVolumeSlider.Value;
        }

        media.Play();

        if (DataContext is MainViewModel vm && !vm.IsAutoPlay)
        {
            // Delay pause to let first frame render, then show as thumbnail
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                media.Pause();
                media.Position = TimeSpan.FromMilliseconds(100);
            });
        }
    }

    private void PlayMedia_Click(object sender, RoutedEventArgs e) => VideoPlayer.Play();

    private void PauseMedia_Click(object sender, RoutedEventArgs e) => VideoPlayer.Pause();

    private void StopMedia_Click(object sender, RoutedEventArgs e) => VideoPlayer.Stop();

    private void PlayAudio_Click(object sender, RoutedEventArgs e) => AudioPlayer.Play();

    private void PauseAudio_Click(object sender, RoutedEventArgs e) => AudioPlayer.Pause();

    private void StopAudio_Click(object sender, RoutedEventArgs e) => AudioPlayer.Stop();

    private double _videoVolumeBeforeMute = 0.5;
    private double _audioVolumeBeforeMute = 0.5;

    private void VideoVolume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        VideoPlayer.Volume = e.NewValue;
        VideoMuteButton.Content = e.NewValue < 0.01 ? "🔇" : "🔊";
    }

    private void AudioVolume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AudioPlayer.Volume = e.NewValue;
        AudioMuteButton.Content = e.NewValue < 0.01 ? "🔇" : "🔊";
    }

    private void VideoMute_Click(object sender, RoutedEventArgs e)
    {
        if (VideoVolumeSlider.Value > 0.01)
        {
            _videoVolumeBeforeMute = VideoVolumeSlider.Value;
            VideoVolumeSlider.Value = 0;
        }
        else
        {
            VideoVolumeSlider.Value = _videoVolumeBeforeMute;
        }
    }

    private void AudioMute_Click(object sender, RoutedEventArgs e)
    {
        if (AudioVolumeSlider.Value > 0.01)
        {
            _audioVolumeBeforeMute = AudioVolumeSlider.Value;
            AudioVolumeSlider.Value = 0;
        }
        else
        {
            AudioVolumeSlider.Value = _audioVolumeBeforeMute;
        }
    }

    private void DuplicateGroups_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Stop any playing media when selection changes
        try
        {
            VideoPlayer.Stop();
            AudioPlayer.Stop();
        }
        catch
        {
            // Media elements may not be initialized yet
        }
    }
}
