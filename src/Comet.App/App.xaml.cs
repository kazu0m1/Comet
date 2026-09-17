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

        var settingsStore = new JsonSettingsStore();
        var stateStore = new JsonBookStateStore();
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
