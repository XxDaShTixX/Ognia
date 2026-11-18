using System.IO;

namespace Ognia.Frontend.Helpers;

public static class AppPaths
{
    public static string AppDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ognia");

    public static string DefaultMusicRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Ognia");

    public static void EnsureAppDataFolder()
    {
        Directory.CreateDirectory(AppDataFolder);
    }
}