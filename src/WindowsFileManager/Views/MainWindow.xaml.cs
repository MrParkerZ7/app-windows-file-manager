using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using WindowsFileManager.Core.Models;
using WindowsFileManager.ViewModels;

namespace WindowsFileManager.Views;

/// <summary>
/// Main window code-behind.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    private double _videoVolumeBeforeMute = 0.5;
    private double _audioVolumeBeforeMute = 0.5;
    private GridViewColumnHeader? _lastFolderSortHeader;
    private ListSortDirection _lastFolderSortDirection = ListSortDirection.Ascending;
    private bool _savedPreviewVisible;
    private bool _savedAnalyticsVisible;
    private bool _isWindowLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            var settings = vm.GetSettings();
            if (settings.WindowWidth != null && settings.WindowHeight != null)
            {
                var left = settings.WindowLeft ?? 0;
                var top = settings.WindowTop ?? 0;
                var width = settings.WindowWidth.Value;
                var height = settings.WindowHeight.Value;

                // Check if the saved position is visible on any current monitor
                // Uses virtual screen bounds (spans all monitors)
                var isOnScreen = left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth &&
                                 left + width > SystemParameters.VirtualScreenLeft &&
                                 top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight &&
                                 top + height > SystemParameters.VirtualScreenTop;

                if (isOnScreen)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = left;
                    Top = top;
                    Width = width;
                    Height = height;
                }

                if (settings.IsMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
            }

            // Save initial panel states for tab switching
            _savedPreviewVisible = vm.IsPreviewVisible;
            _savedAnalyticsVisible = vm.IsAnalyticsVisible;

            // Folder Control is the first tab — hide panels on startup
            vm.IsPreviewVisible = false;
            vm.IsAnalyticsVisible = false;
            vm.IsFolderControlActive = true;
        }

        _isWindowLoaded = true;
    }

    private void CloseAnalytics_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsAnalyticsVisible = false;
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            // Save window state using RestoreBounds (gives normal size even when maximized)
            var bounds = WindowState == WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);
            vm.SaveWindowState(bounds.Left, bounds.Top, bounds.Width, bounds.Height, WindowState == WindowState.Maximized);
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

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isWindowLoaded || sender is not TabControl tabControl || e.Source != tabControl || DataContext is not MainViewModel vm)
        {
            return;
        }

        if (tabControl.SelectedItem is TabItem tab && tab.Header as string == "Folder Control")
        {
            // Save current panel states and hide them
            _savedPreviewVisible = vm.IsPreviewVisible;
            _savedAnalyticsVisible = vm.IsAnalyticsVisible;
            vm.IsPreviewVisible = false;
            vm.IsAnalyticsVisible = false;
            vm.IsFolderControlActive = true;
        }
        else
        {
            // Restore panel states when switching back
            vm.IsFolderControlActive = false;
            vm.IsPreviewVisible = _savedPreviewVisible;
            vm.IsAnalyticsVisible = _savedAnalyticsVisible;
        }
    }

    private void FolderResults_ColumnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Role == GridViewColumnHeaderRole.Padding)
        {
            return;
        }

        // Determine the sort property from the column binding
        string? sortBy = null;
        if (header.Column?.DisplayMemberBinding is Binding binding)
        {
            sortBy = binding.Path.Path;
        }
        else if (header.Column?.CellTemplate != null)
        {
            // For template columns, use the header text as a hint
            var headerText = header.Column.Header as string;
            sortBy = headerText?.TrimEnd(' ', '▲', '▼') switch
            {
                "Full Path" => "FullPath",
                "Size" => "TotalSize",
                _ => null,
            };
        }

        if (sortBy == null)
        {
            return;
        }

        // Toggle direction if same column clicked again
        var direction = ListSortDirection.Ascending;
        if (header == _lastFolderSortHeader)
        {
            direction = _lastFolderSortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }

        _lastFolderSortHeader = header;
        _lastFolderSortDirection = direction;

        if (sender is ListView listView)
        {
            var view = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(sortBy, direction));

            // Update header text with sort indicator
            foreach (var col in ((GridView)listView.View).Columns)
            {
                if (col.Header is string h)
                {
                    col.Header = h.TrimEnd(' ', '▲', '▼');
                }
            }

            var arrow = direction == ListSortDirection.Ascending ? " ▲" : " ▼";
            if (header.Column?.Header is string currentHeader)
            {
                header.Column.Header = currentHeader.TrimEnd(' ', '▲', '▼') + arrow;
            }
        }
    }

    private void SubfolderPrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is SubfolderItem item)
        {
            item.PrevPage();
        }
    }

    private void SubfolderNextPage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is SubfolderItem item)
        {
            item.NextPage();
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
