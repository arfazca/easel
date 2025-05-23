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
    public async Task RunNonInteractiveAsync(
        string templateName,
        string position,
        string companyName,
        string companySuffix,
        string division,
        string city,
        string state,
        string terms,
        bool hasReference,
        string? referenceName,
        string? referenceTitle)
    {
        try
        {
            // Console.WriteLine($"Template: {templateName}");
            // Console.WriteLine($"Position: {position}");
            // Console.WriteLine($"Company: {companyName}");
            // Console.WriteLine($"Suffix: {companySuffix}");
            // Console.WriteLine($"Division: {division}");
            // Console.WriteLine($"City: {city}");
            // Console.WriteLine($"State: {state}");
            // Console.WriteLine($"Terms: {terms}");
            // Console.WriteLine($"HasReference: {hasReference}");
            // Console.WriteLine($"ReferenceName: {referenceName}");
            // Console.WriteLine($"ReferenceTitle: {referenceTitle}");

            var termPeriods = pathProvider.GetTermPeriods();
            if (termPeriods.Length == 0)
            {
                throw new InvalidOperationException("No term periods configured");
            }

            var appData = new ApplicationData
            {
                Position = position,
                CompanyName = companyName,
                CompanySuffix = companySuffix,
                Division = division,
                City = city,
                State = state,
                Terms = terms,
                UpTerm = termPeriods.Length > 0 ? termPeriods[0] : "DEFAULT TERM",
                HasReference = hasReference,
                ReferenceName = referenceName,
                ReferenceTitle = referenceTitle
            };

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