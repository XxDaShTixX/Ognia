using Ognia.Wpf.Services;

namespace Ognia.Wpf.Helpers;

public static class AppInfo
{
    public const string GitHubRepositoryUrl = "https://github.com/XxDaShTixX/Ognia-WPF";
    public static string Version => UpdateService.Instance.CurrentVersion;
    public static string DisplayVersion => UpdateService.Instance.DisplayVersion;
}