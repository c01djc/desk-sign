using System.IO;
using System.Text.Json;

namespace DeskSign.Services;

public sealed class UserSettings
{
    public string FontName { get; set; } = "宋体";
    public double FontSizePt { get; set; } = 150;
    public double SpacingCm { get; set; } = 14;
    public double GuideOffsetCm { get; set; } = 0.5;
    public bool ShowFoldLine { get; set; } = true;
    public string? LastDirectory { get; set; }
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "c0ldjc",
        "桌签生成器",
        "settings.json");

    public static UserSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new UserSettings();

            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath)) ?? new UserSettings();
        }
        catch (IOException)
        {
            return new UserSettings();
        }
        catch (JsonException)
        {
            return new UserSettings();
        }
    }

    public static void Save(UserSettings settings)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
