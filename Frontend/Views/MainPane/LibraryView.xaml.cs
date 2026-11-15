using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ognia.Frontend.ViewModels;

namespace Ognia.Frontend.Views.MainPane;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }

    private void CreatePlaylist_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is LibraryViewModel vm)
            vm.BeginCreatePlaylistCommand.Execute(null);
    }
}