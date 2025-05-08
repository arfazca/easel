using easel.Core.Interface;
using easel.CLI.Interface;
using easel.Core.Models;
using easel.Infrastructure.Interface;

namespace easel.CLI;

public class ApplicationManager(
    ITemplateService templateService,
    IConsoleInterfaceService consoleInterface,
    IBasePathProvider pathProvider)
{
    public async Task RunAsync()
    {
        try
        {
            // Collect user input
            var termPeriods = pathProvider.GetTermPeriods();
            if (termPeriods.Length == 0)
            {
                throw new InvalidOperationException("No term periods configured");
            }
        
            var templateName = await consoleInterface.SelectTemplate();
            ApplicationData appData;
        
            if (templateName.Equals("TEST", StringComparison.OrdinalIgnoreCase))
            {
                appData = pathProvider.GetTestData();
            }
            else
            {
                appData = await consoleInterface.CollectApplicationData(termPeriods);
            }
        
            // Generate document
            var workingDir = await templateService.GenerateDocument(appData, templateName);
        
            // Compile to PDF
            var outputPath = await templateService.CompileToPdf(
                workingDir, 
                appData.GenerateFileName(),
                appData);
            
            Console.WriteLine($"Generated: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }
}