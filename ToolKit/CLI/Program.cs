using easel.CLI.Interface;
using easel.Core.Interface;
using easel.Infrastructure.Interface;

using easel.CLI.Services;
using easel.Core.Services;
using easel.Core.Models;

using System.CommandLine;
using Microsoft.Extensions.Hosting;
using easel.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace easel.CLI;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--non-interactive" || args[0] == "-n"))
        {
            await RunNonInteractiveMode(args);
            return;
        }
        
        var host = CreateHostBuilder(args).Build();
        await host.Services.GetRequiredService<ApplicationManager>().RunAsync();
    }
    
    private static async Task RunNonInteractiveMode(string[] args)
    {
        var commandArgs = args.Where(arg => arg != "--non-interactive" && arg != "-n").ToArray();
        var rootCommand = new RootCommand("Generate cover letters non-interactively");
        
        var templateOption = new Option<string>(
            name: "--template",
            description: "Template name to use");
        
        var positionOption = new Option<string>(
            name: "--position",
            description: "Position title");
            
        var companyOption = new Option<string>(
            name: "--company",
            description: "Company name");
            
        var suffixOption = new Option<string>(
            name: "--suffix",
            description: "Company suffix",
            getDefaultValue: () => string.Empty);
            
        var divisionOption = new Option<string>(
            name: "--division",
            description: "Division/department");
            
        var cityOption = new Option<string>(
            name: "--city",
            description: "Location city");
            
        var stateOption = new Option<string>(
            name: "--state",
            description: "Location state/province");
            
        var termsOption = new Option<string>(
            name: "--terms",
            description: "Term length");

        rootCommand.AddOption(templateOption);
        rootCommand.AddOption(positionOption);
        rootCommand.AddOption(companyOption);
        rootCommand.AddOption(suffixOption);
        rootCommand.AddOption(divisionOption);
        rootCommand.AddOption(cityOption);
        rootCommand.AddOption(stateOption);
        rootCommand.AddOption(termsOption);

        rootCommand.SetHandler<string, string, string, string, string, string, string, string>(
            async (template, position, company, suffix, division, city, state, terms) => 
        {
            var host = CreateHostBuilder(args).Build();
            var appManager = host.Services.GetRequiredService<ApplicationManager>();
            await appManager.RunNonInteractiveAsync(template, position, company, suffix, division, city, state, terms);
        }, 
        templateOption, positionOption, companyOption, suffixOption, divisionOption, cityOption, stateOption, termsOption);

        await rootCommand.InvokeAsync(commandArgs);
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((config) =>
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
            });
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