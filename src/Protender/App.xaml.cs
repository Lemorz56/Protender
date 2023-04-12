using Serilog;
using System;
using System.Configuration;
using System.Windows;

namespace Protender;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        MainWindow = new MainWindow();

        Console.WriteLine(ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString);
        // Logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/Protender.txt")
            .CreateLogger();

        try
        {
            MainWindow.ShowDialog();
        }
        catch (Exception e)
        {
            Log.Error(e, "Fatal Error");
            throw;
        }
    }
}