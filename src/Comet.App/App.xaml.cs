using System.Windows;
using Comet.Infrastructure.Navigation;
using Comet.Infrastructure.Persistence;
using Comet.Infrastructure.Sources;

namespace Comet.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Explicit development/test profile isolation; production defaults stay unchanged.
        var profile = Environment.GetEnvironmentVariable("COMET_PROFILE_DIR");
        if (string.IsNullOrWhiteSpace(profile)) profile = null;
        var settingsStore = new JsonSettingsStore(profile);
        var stateStore = new JsonBookStateStore(profile);
        var settings = await settingsStore.LoadAsync();
        var window = new MainWindow(
            new BookSourceFactory(),
            settingsStore,
            stateStore,
            new AdjacentArchiveFinder(),
            settings);
        MainWindow = window;
        window.Show();

        if (e.Args.Length > 0)
            await window.OpenPathAsync(e.Args[0], Comet.Core.Models.BookOpenReason.StartupArgument);
    }
}
