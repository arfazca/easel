using System.Reflection;
using easel.CLI.Interface;
using easel.Core.Interface;
using easel.Infrastructure.Interface;

using easel.CLI.Services;
using easel.Core.Services;
using easel.Core.Models;

using Microsoft.Extensions.Hosting;
using easel.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace easel.CLI;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // Find appsettings.json in likely locations (Support for my build script)
                var baseDirectory = AppContext.BaseDirectory;
                var possiblePaths = new[]
                {
                    Path.Combine(baseDirectory, "ToolKit", "appsettings.json"),
                    Path.Combine(baseDirectory, "appsettings.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "ToolKit", "appsettings.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
                };

                var appSettingsPath = possiblePaths.FirstOrDefault(File.Exists);
                if (appSettingsPath == null)
                {
                    throw new FileNotFoundException("Could not find appsettings.json in any known location");
                }

                // Get the directory containing the config file
                var configDirectory = Path.GetDirectoryName(appSettingsPath);
                if (string.IsNullOrEmpty(configDirectory))
                {
                    // Fallback to base directory if there are issues fetching config file <appsettings.json>
                    configDirectory = baseDirectory;
                }

                config.SetBasePath(configDirectory);
                config.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices(services =>
            {
                // BasePath
                services.AddSingleton<IBasePathProvider, BasePathProvider>();
                
                // Core Services
                services.AddSingleton<TemplateVariableService>();
                
                // Infrastructure
                services.AddSingleton<IFileSystemService, FileSystemService>();
                services.AddSingleton<LatexCompiler>();
                
                // Composite Template Service
                services.AddSingleton<ITemplateService>(provider => 
                    new CompositeTemplateService(
                        provider.GetRequiredService<TemplateVariableService>(),
                        provider.GetRequiredService<LatexCompiler>()));
                
                // UI & Application
                services.AddSingleton<IConsoleInterfaceService, ConsoleInterfaceService>();
                services.AddSingleton<ApplicationManager>();
            })
            .Build();
        await host.Services.GetRequiredService<ApplicationManager>().RunAsync();
    }
}

// Composite services that combines both template generation and compilation
public class CompositeTemplateService(TemplateVariableService generator, LatexCompiler compiler) : ITemplateService
{
    public async Task<string> GenerateDocument(ApplicationData data, string templateName)
        => await generator.GenerateDocument(data, templateName);

    public async Task<string> CompileToPdf(string workingDirectory, string outputFileName, ApplicationData appData)
        => await compiler.CompileToPdf(workingDirectory, outputFileName, appData);
}

