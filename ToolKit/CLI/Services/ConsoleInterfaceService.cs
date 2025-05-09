using System.Diagnostics;
using easel.CLI.Interface;
using easel.Core.Models;
using easel.Infrastructure.Interface;

namespace easel.CLI.Services;

public class ConsoleInterfaceService(IBasePathProvider pathProvider) : IConsoleInterfaceService
{
    public async Task<ApplicationData> CollectApplicationData(string[] termPeriods)
    {
        var data = new ApplicationData
        {
            Position = await SelectWithFzf(
                "Select Position",
                Path.Combine(pathProvider.GetDataPath(), "Titles.txt")
            ) ?? "Software Engineering",

            CompanyName = await SelectWithFzf(
                "Select Company",
                Path.Combine(pathProvider.GetDataPath(), "Company Names.txt")
            ) ?? "Example Company",

            CompanySuffix = await SelectWithFzf(
                "Select Company Suffix",
                Path.Combine(pathProvider.GetDataPath(), "Company Suffix.txt")
            ) ?? "",

            Division = await SelectWithFzf(
                "Select Division",
                Path.Combine(pathProvider.GetDataPath(), "Company Divisions.txt")
            ) ?? "Human Resources"
        };

        var locationDataPath = Path.Combine(pathProvider.GetDataPath(), "Location Data");
        var provinceFiles = Directory.GetFiles(locationDataPath, "*.txt")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null && !name.Equals("Provinces", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var province = await SelectWithFzf(
            "Select Province/State",
            options: provinceFiles! 
        ) ?? "British Columbia";

        data.State = province;
        data.City = await SelectWithFzf(
            $"Select City in {province}",
            Path.Combine(locationDataPath, $"{province}.txt")
        ) ?? "Vancouver";

        data.Terms = await SelectWithFzf(
            "Select Term Length",
            Path.Combine(pathProvider.GetDataPath(), "Term Lengths.txt")
        ) ?? "8 months";

        data.UpTerm = termPeriods.Length > 0
            ? termPeriods[0]
            : throw new InvalidOperationException("No term periods configured");

        Console.WriteLine("Do you have a specific reference for this job? (y/n)");
        var hasReference = Console.ReadLine()?.Trim().ToLower() == "y";
        data.HasReference = hasReference;

        if (hasReference)
        {
            Console.Write("Reference Name: ");
            data.ReferenceName = Console.ReadLine()?.Trim();
            
            Console.Write("Reference Title: ");
            data.ReferenceTitle = Console.ReadLine()?.Trim();
            
            data.ReferenceCity = data.City;
            data.ReferenceState = data.State;
        }

        return data;
    }

    public async Task<string> SelectTemplate()
    {
        var templateDir = pathProvider.GetTemplatesPath();
        var templates = Directory.GetDirectories(templateDir)
            .Select(Path.GetFileName)
            .Where(x => x != null && !x.Equals("Temporary", StringComparison.OrdinalIgnoreCase))
            .ToList();

        templates.Add("TEST"); 
    
        if (templates.Count == 0)
        {
            throw new InvalidOperationException($"No templates found in {templateDir}");
        }

        var selected = await SelectWithFzf(
            "Select Template", 
            options: templates! // Non-null assertion since we filtered nulls
        );
    
        return selected ?? templates[0]!;
    }

    private async Task<string?> SelectWithFzf(
    string prompt, 
    string? filePath = null, 
    List<string>? options = null,
    bool allowAddOption = true)
    {
        string[] inputLines = options is not null 
            ? [.. options]
            : filePath is not null 
                ? await File.ReadAllLinesAsync(filePath) 
                : [];

        // Only allow adding new options if:
        // 1. The flag is true
        // 2. Not selecting a province/state
        // 3. Not selecting a template
        bool isProvinceSelection = prompt.Contains("Province/State");
        bool isTemplateSelection = prompt.Contains("Template");
        bool shouldAllowAdd = allowAddOption && !isProvinceSelection && !isTemplateSelection;

        if (shouldAllowAdd)
        {
            inputLines = inputLines.Append("+++++++++++++++++++++ ADD NEW OPTION +++++++++++++++++++++")
                                  .Append("ESC - EXIT")
                                  .ToArray();
        }
        else
        {
            inputLines = inputLines.Append("ESC - EXIT").ToArray();
        }

        if (inputLines.Length == 0) return null;

        var psi = new ProcessStartInfo
        {
            FileName = "fzf",
            Arguments = $"--prompt=\"{prompt}> \" --bind=esc:cancel",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? 
                            throw new InvalidOperationException("Failed to start fzf process");

        await using (var writer = process.StandardInput)
        {
            foreach (var line in inputLines)
            {
                await writer.WriteLineAsync(line);
            }
        }

        var selected = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        selected = selected.Trim();

        // Check if ESC was pressed or EXIT was selected
        if (string.IsNullOrEmpty(selected) || selected.Equals("ESC - EXIT", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Operation cancelled by user.");
            Environment.Exit(0);
        }

        if (selected.StartsWith("+++++++++++++++++++++"))
        {
            Console.Write("Enter new option: ");
            var newOption = Console.ReadLine()?.Trim();
            
            if (!string.IsNullOrEmpty(newOption))
            {
                // if option already exists
                if (inputLines.Contains(newOption, StringComparer.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"'{newOption}' already exists - not added to list.");
                    Console.ResetColor();
                    return newOption;
                }

                // Append to file if filePath was provided
                if (filePath != null)
                {
                    await File.AppendAllLinesAsync(filePath, new[] { newOption });
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Added '{newOption}'");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Using '{newOption}' (not saved to file)");
                    Console.ResetColor();
                }
                
                return newOption;
            }
            return null;
        }

        return string.IsNullOrWhiteSpace(selected) ? null : selected;
    }
    
    public ApplicationData CreateApplicationDataFromArgs(
        string position, string companyName, string companySuffix,
        string division, string city, string state, string terms, string[] termPeriods)
    {
        return new ApplicationData
        {
            Position = position,
            CompanyName = companyName,
            CompanySuffix = companySuffix,
            Division = division,
            City = city,
            State = state,
            Terms = terms,
            UpTerm = termPeriods.Length > 0 ? termPeriods[0] : "DEFAULT TERM"
        };
    }
}