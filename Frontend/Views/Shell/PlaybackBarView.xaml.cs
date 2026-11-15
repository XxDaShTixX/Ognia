using Ognia.Frontend.ViewModels;
using System.Windows.Controls;

namespace Ognia.Frontend.Views.Shell;

public partial class PlaybackBarView : UserControl
{
    public PlaybackBarView()
    {
        InitializeComponent();
        Unloaded += (_, _) =>
        {
            if (DataContext is PlaybackBarViewModel vm)
                vm.PlaybackViewModel.Dispose();
        };
    }

    private void Slider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
    {
        if (DataContext is PlaybackBarViewModel vm)
            vm.PlaybackViewModel.BeginSeek();
    }

    private void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        if (DataContext is PlaybackBarViewModel vm && sender is Slider slider)
            vm.PlaybackViewModel.EndSeek(TimeSpan.FromSeconds(slider.Value));
    }

    private void Thumbnail_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not PlaybackBarViewModel vm) return;

        var url = vm.PlaybackViewModel.CurrentTrack?.SourceUrl;
        if (string.IsNullOrWhiteSpace(url)) return;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}