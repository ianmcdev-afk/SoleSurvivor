using System.Windows;
using System.Windows.Threading;

namespace SoleSurvivor.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Keep a stray animation/UI bug from killing the game silently.
        DispatcherUnhandledException += OnUnhandled;
    }

    private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Something went wrong:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}", "Sole Survivor",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
