using System.Windows.Controls;

namespace Ognia.Frontend.Views.Shell;

/// <summary>
/// Interaction logic for Sidebar.xaml
/// </summary>
public partial class MainPaneView : UserControl
{
    public MainPaneView()
    {
        InitializeComponent();
    }

    public object? Content
    {
        get => HostContent.Content;
        set => HostContent.Content = value;
    }
}
