using System.Reflection;
using Velopack;

namespace Ognia.Wpf.Helpers;

public static class AppInfo
{
    public const string GitHubRepositoryUrl = "https://github.com/XxDaShTixX/Ognia-WPF";

    /// <summary>
    /// The installed Velopack version, or the assembly version when running
    /// outside an install (e.g. from bin/ during development).
    /// </summary>
    public static string Version
    {
        get
        {
            try
            {
                var mgr = new UpdateManager(
                    new Velopack.Sources.GithubSource(
                        GitHubRepositoryUrl, accessToken: null, prerelease: false));

                if (mgr.IsInstalled && mgr.CurrentVersion != null)
                    return mgr.CurrentVersion.ToString();
            }
            catch { }

            var asm = Assembly.GetExecutingAssembly().GetName().Version;
            return asm == null ? "0.0.0" : $"{asm.Major}.{asm.Minor}.{asm.Build}";
        }
    }

    public static string DisplayVersion => $"Ognia v{Version}";
}