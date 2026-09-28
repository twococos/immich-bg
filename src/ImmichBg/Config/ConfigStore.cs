using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImmichBg.Logging;

namespace ImmichBg.Config;

public static class ConfigStore
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(AppPaths.ConfigFile))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(AppPaths.ConfigFile), JsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            AppLog.Error($"config.json: {ex.Message}");
        }
        return new AppConfig();
    }

    public static void Save(AppConfig config) =>
        AppPaths.WriteAllTextAtomic(AppPaths.ConfigFile, JsonSerializer.Serialize(config, JsonOptions));

    public static string? GetApiKey(AppConfig config)
    {
        if (string.IsNullOrEmpty(config.ApiKeyProtected)) return null;
        try
        {
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(config.ApiKeyProtected), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            AppLog.Error($"API key: {ex.Message}");
            return null;
        }
    }

    public static void SetApiKey(AppConfig config, string apiKey)
    {
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey.Trim()), null, DataProtectionScope.CurrentUser);
        config.ApiKeyProtected = Convert.ToBase64String(bytes);
    }
}
