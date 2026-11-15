using System.Windows;
using System.Windows.Controls;

namespace Ognia.Frontend.Views.Shell;

public partial class RightPaneView : UserControl
{
    public event Action? CloseRequested;

    public RightPaneView()
    {
        InitializeComponent();
    }

    public object? HostedContent
    {
        get => HostContent.Content;
        set => HostContent.Content = value;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => CloseRequested?.Invoke();
}