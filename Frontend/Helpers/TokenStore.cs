using System.IO;
using System.Text.Json;

namespace Ognia.Frontend.Helpers;

public static class TokenStore
{
    private static readonly string TokenPath =
        Path.Combine(AppPaths.AppDataFolder, "token.json");

    private static PersistedToken? _current;

    private static PersistedToken? Current
    {
        get
        {
            if (_current != null) return _current;
            _current = Load();
            return _current;
        }
    }

    public static string? Token => Current?.Token;
    public static string? Email => Current?.Email;
    public static string? RefreshToken => Current?.RefreshToken;
    public static DateTime? AccessTokenExpiresAt => Current?.AccessTokenExpiresAt;

    public static bool IsAuthenticated =>
    Current != null
    && !string.IsNullOrEmpty(Current.Token)
    && !string.IsNullOrEmpty(Current.RefreshToken);

    public static void Set(string token, string email, DateTime accessTokenExpiresAt, string refreshToken)
    {
        _current = new PersistedToken
        {
            Token = token,
            Email = email,
            AccessTokenExpiresAt = DateTime.SpecifyKind(accessTokenExpiresAt, DateTimeKind.Utc),
            RefreshToken = refreshToken
        };
        Save(_current);
    }

    public static void Clear()
    {
        _current = null;
        try { if (File.Exists(TokenPath)) File.Delete(TokenPath); } catch { }
    }

    private static PersistedToken? Load()
    {
        try
        {
            if (!File.Exists(TokenPath)) return null;
            var json = File.ReadAllText(TokenPath);
            return JsonSerializer.Deserialize<PersistedToken>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void Save(PersistedToken token)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.AppDataFolder);
            File.WriteAllText(TokenPath,
                JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Non-fatal — the user just won't stay logged in across restarts.
        }
    }

    private class PersistedToken
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
    }
}