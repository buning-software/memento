using Memento.Core.Storage;

namespace Memento.Core.UnitTests.Storage;

public class MementoPathsTests
{
    [Fact]
    public void The_database_and_settings_files_live_in_the_data_directory()
    {
        var paths = new MementoPaths(Path.Combine("data", "folder"));

        Assert.Equal(Path.Combine("data", "folder", "memento.db"), paths.DatabasePath);
        Assert.Equal(Path.Combine("data", "folder", "settings.json"), paths.SettingsPath);
    }

    [Fact]
    public void A_trailing_separator_in_the_data_directory_does_not_double_up()
    {
        string directory = Path.Combine("data", "folder") + Path.DirectorySeparatorChar;
        var paths = new MementoPaths(directory);

        Assert.Equal(Path.Combine("data", "folder", "memento.db"), paths.DatabasePath);
    }

    [Fact]
    public void The_current_user_folder_is_Memento_under_the_roaming_app_data()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        Assert.Equal(Path.Combine(appData, "Memento"), MementoPaths.ForCurrentUser().DataDirectory);
    }

    [Fact]
    public void The_current_user_folder_can_be_named_differently()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var paths = MementoPaths.ForCurrentUser("Memento-Debug");

        Assert.Equal(Path.Combine(appData, "Memento-Debug"), paths.DataDirectory);
        Assert.Equal(Path.Combine(appData, "Memento-Debug", "settings.json"), paths.SettingsPath);
    }

    [Fact]
    public void Paths_with_the_same_directory_are_equal()
    {
        Assert.Equal(new MementoPaths("x"), new MementoPaths("x"));
        Assert.NotEqual(new MementoPaths("x"), new MementoPaths("y"));
    }
}
