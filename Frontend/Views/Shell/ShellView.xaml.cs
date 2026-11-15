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

    public void SetPlaybackContent(object view) => PlaybackContent.Content = view;

    public void SetMainContent(object view) => MainPane.Content = view;

    public void SetRightContent(object? view)
    {
        RightPane.Content = view;
        RightPane.Visibility = view == null ? Visibility.Collapsed : Visibility.Visible;
        RightColumn.Width = view == null ? new GridLength(0) : new GridLength(250);
    }
}