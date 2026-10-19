namespace Memento.Core.Storage;

/// <summary>Where Memento keeps its files. The app uses %APPDATA%\Memento; tests use a temp folder.</summary>
public sealed record MementoPaths(string DataDirectory)
{
    public string DatabasePath => Path.Combine(DataDirectory, "memento.db");

    public string SettingsPath => Path.Combine(DataDirectory, "settings.json");

    /// <summary>%APPDATA%\<paramref name="folderName"/>. Debug builds pass a different name so development never touches real data.</summary>
    public static MementoPaths ForCurrentUser(string folderName = "Memento") =>
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), folderName));
}
