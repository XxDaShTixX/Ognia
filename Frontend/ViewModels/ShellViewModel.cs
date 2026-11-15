using Ognia.Frontend.Navigation;
using Ognia.Frontend.Session;

namespace Ognia.Frontend.ViewModels;

/// <summary>
/// Composes the left pane's data context. Exists so ShellView has one
/// DataContext root, and future pane VMs (queue, top bar) slot in here.
/// </summary>
public sealed class ShellViewModel
{
    public ShellViewModel(AppSession session, IShellNavigator nav)
    {
        Session = session;
        Left = new LeftPaneViewModel(session, nav);
    }

    public AppSession Session { get; }
    public LeftPaneViewModel Left { get; }
}