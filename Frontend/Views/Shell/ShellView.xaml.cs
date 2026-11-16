using System.Windows;
using System.Windows.Controls;

namespace Ognia.Frontend.Views.Shell;

public partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();
        RightPane.CloseRequested += () => SetRightContent(null);
    }

    public object? PlaybackBarDataContext => PlaybackContent.Content is PlaybackBarView pb
        ? pb.DataContext
        : null;

    public void SetPlaybackContent(object view) => PlaybackContent.Content = view;

    public void SetMainContent(object view) => MainPane.HostedContent = view;

    public void SetRightContent(object? view)
    {
        RightPane.HostedContent = view;
        RightPane.Visibility = view == null ? Visibility.Collapsed : Visibility.Visible;
        RightColumn.Width = view == null ? new GridLength(0) : new GridLength(250);
    }
}