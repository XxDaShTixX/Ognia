using Ognia.Frontend.Enums;
using Ognia.Frontend.Helpers;
using Ognia.Frontend.Navigation;
using Ognia.Frontend.Repos;
using Ognia.Frontend.Services;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Ognia.Frontend;

public partial class MainWindow : Window
{
    public static MainWindow? Current { get; private set; }

    public ApiClientService Api { get; }
    public IAppNavigator Navigator { get; }

    public MainWindow()
    {
        InitializeComponent();
        Current = this;

        Api = new ApiClientService(ApiConfig.BaseUrl);
        Navigator = new AppNavigator(Api, view => MainContent.Content = view);

        Api.Unauthorized += OnUnauthorized;

        _ = InitializeAuthAsync();
    }

    #region Round Edges
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int preference = 2;
        DwmSetWindowAttribute(hwnd, 33, ref preference, sizeof(int));
    }
    #endregion

    private async Task InitializeAuthAsync()
    {
        if (AppSettings.Current.Mode == AppMode.Guest)
        {
            Navigator.EnterGuestMode();
            return;
        }

        if (!TokenStore.IsAuthenticated)
        {
            Navigator.ShowLogin();
            return;
        }

        var ok = await Api.TryRefreshAsync();
        if (ok)
            Navigator.ShowShell(new ApiMusicRepo(Api));
        else
            Navigator.ShowLogin();
    }

    private void OnUnauthorized()
    {
        TokenStore.Clear();
        Application.Current.Dispatcher.Invoke(Navigator.ShowLogin);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Navigator.DisposeShell();
        Close();
    }
}