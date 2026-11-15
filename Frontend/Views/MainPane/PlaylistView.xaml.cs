using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ognia.Frontend.ViewModels;

namespace Ognia.Frontend.Views.MainPane;

public partial class PlaylistView : UserControl
{
    private Point _dragStart;
    private TrackRowViewModel? _dragItem;

    public PlaylistView() => InitializeComponent();

    private void TracksList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(TracksList);
        _dragItem = GetItemUnderMouse(e.OriginalSource as DependencyObject);
    }

    private void TracksList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragItem == null) return;

        var pos = e.GetPosition(TracksList);
        if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        DragDrop.DoDragDrop(TracksList, _dragItem, DragDropEffects.Move);
        _dragItem = null;
    }

    private void TracksList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(TrackRowViewModel))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void TracksList_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not PlaylistViewModel vm) return;
        if (vm.Session.IsBusy) return;
        if (!e.Data.GetDataPresent(typeof(TrackRowViewModel))) return;

        var dragged = (TrackRowViewModel)e.Data.GetData(typeof(TrackRowViewModel))!;
        var target = GetItemUnderMouse(e.OriginalSource as DependencyObject);

        var session = vm.Session;
        var tracks = session.SelectedPlaylistTracks;

        var fromIndex = tracks.IndexOf(dragged);
        if (fromIndex < 0) return;

        int toIndex = target != null ? tracks.IndexOf(target) : tracks.Count - 1;
        if (toIndex < 0 || toIndex == fromIndex) return;

        // Move within the ObservableCollection for immediate visual feedback.
        tracks.Move(fromIndex, toIndex);
        session.ReindexTracks();

        // Persist the new order.
        var orderedIds = tracks.Select(t => t.Id).ToList();
        var playlistId = session.SelectedPlaylist?.Id;
        if (playlistId == null) return;

        _ = PersistOrderAsync(vm, playlistId.Value, orderedIds);
    }

    private static async Task PersistOrderAsync(PlaylistViewModel vm, int playlistId, List<int> orderedIds)
    {
        var result = await vm.Session.Repo.ReorderTracksAsync(playlistId, orderedIds);
        if (!result.Success)
            vm.Session.SetError(result.Error ?? "Failed to reorder tracks.");
    }

    private static TrackRowViewModel? GetItemUnderMouse(DependencyObject? source)
    {
        while (source != null && source is not ListBoxItem)
            source = System.Windows.Media.VisualTreeHelper.GetParent(source);

        return (source as ListBoxItem)?.DataContext as TrackRowViewModel;
    }
}