using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Desktop.Views;

namespace UniversityDepartmentMIS.Desktop;

public partial class App : Application
{
    public const string ErrorCaption = "University Department MIS - error";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppServices services;
        try
        {
            services = AppServices.Create(DatabasePaths.Resolve());
        }
        catch (Exception ex)
        {
            Report("The database could not be opened or created.", ex);
            Shutdown(1);
            return;
        }

        var login = new LoginWindow(services);
        MainWindow = login;
        login.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Report("Something went wrong. The details were written to the error log.", e.Exception);
        e.Handled = true;
    }

    /// <summary>Writes the exception next to the database file and tells the user.</summary>
    private static void Report(string message, Exception exception)
    {
        try
        {
            var logPath = Path.Combine(Path.GetDirectoryName(DatabasePaths.Resolve()) ?? ".", "error.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath,
                $"{DateTime.Now.ToString("u", CultureInfo.InvariantCulture)} {message}{Environment.NewLine}{exception}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Logging must never hide the original problem.
        }
        catch (UnauthorizedAccessException)
        {
        }

        MessageBox.Show($"{message}{Environment.NewLine}{Environment.NewLine}{exception.Message}", ErrorCaption,
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
