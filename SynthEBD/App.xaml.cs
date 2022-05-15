﻿using System.Windows;

namespace SynthEBD;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    void App_Startup(object sender, StartupEventArgs e)
    {
        // Application is running
        // Process command line args
        bool startMinimized = false;
        for (int i = 0; i != e.Args.Length; ++i)
        {
            if (e.Args[i] == "/StartMinimized")
            {
                startMinimized = true;
            }
        }

        // Create main application window, starting minimized if specified
        MainWindow mainWindow = new MainWindow();
        if (startMinimized)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }
        mainWindow.Show();
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        CustomMessageBox.DisplayNotificationOK("SynthEBD has crashed.", "SynthEBD has crashed with the following error:" + Environment.NewLine + ExceptionLogger.GetExceptionStack(e.Exception, ""));

        var path = System.IO.Path.Combine(PatcherSettings.Paths.LogFolderPath, "Crash Logs", DateTime.Now.ToString("yyyy-MM-dd-HH-mm", System.Globalization.CultureInfo.InvariantCulture) + ".txt");
        PatcherIO.WriteTextFile(path, ExceptionLogger.GetExceptionStack(e.Exception, ""));

        //e.Handled = true;
    }

}