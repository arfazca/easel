using System.Diagnostics;
using easel.Core.Interface;
using easel.Core.Models;
using easel.Infrastructure.Interface;

namespace easel.Infrastructure.Services;

public class LatexCompiler(IFileSystemService fileSystem, IBasePathProvider pathProvider) : ITemplateService
{
    public async Task<string> CompileToPdf(string workingDirectory, string outputFileName, ApplicationData appData)
    {
        var fullWorkingDir = Path.Combine(pathProvider.GetTemplatesPath(), workingDirectory);
        var texFiles = Directory.GetFiles(fullWorkingDir, "*.tex");
        var mainTexFile = "main.tex";

        if (!File.Exists(Path.Combine(fullWorkingDir, mainTexFile)) && texFiles.Length > 0)
        {
            // Use the first .tex file if main.tex doesn't exist
            mainTexFile = Path.GetFileName(texFiles[0]);
        }

        Console.WriteLine($"Compiling {mainTexFile} in {fullWorkingDir}");

        var processInfo = new ProcessStartInfo
        {
            FileName = "pdflatex",
            Arguments = $"-interaction=nonstopmode -output-directory=\"{fullWorkingDir}\" \"{Path.Combine(fullWorkingDir, mainTexFile)}\"",
            WorkingDirectory = fullWorkingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);

        if (process == null)
        {
            throw new Exception("Failed to start pdflatex process");
        }

        // Capture the output for debugging
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Console.WriteLine("LaTeX Output: " + output);
            Console.WriteLine("LaTeX Error: " + error);
            throw new Exception($"LaTeX compilation failed with exit code {process.ExitCode}");
        }

        // Move and organize output files
        var finalPath = fileSystem.OrganizeOutputFiles(workingDirectory, outputFileName, appData);
        return finalPath;
    }

    // Explicit interface implementation
    Task<string> ITemplateService.GenerateDocument(ApplicationData data, string templateName)
    {
        throw new NotImplementedException();
    }
}