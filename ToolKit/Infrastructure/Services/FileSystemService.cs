using easel.Core.Models;
using easel.Infrastructure.Interface;
using iText.Kernel.Pdf;

namespace easel.Infrastructure.Services;

public class FileSystemService : IFileSystemService
{
    private readonly IBasePathProvider _pathProvider;
    
    public FileSystemService(IBasePathProvider pathProvider)
    {
        _pathProvider = pathProvider;
        _pathProvider.EnsureDirectoriesExist();
    }
    
    public void PrepareWorkingDirectory(string workingDir, string templateDir)
    {
        var fullWorkingDir = Path.Combine(_pathProvider.GetTemplatesPath(), "Temporary");
        var fullTemplateDir = Path.Combine(_pathProvider.GetTemplatesPath(), templateDir);
    
        // Clear the Temporary directory
        if (Directory.Exists(fullWorkingDir))
        {
            Directory.Delete(fullWorkingDir, true);
        }
    
        Directory.CreateDirectory(fullWorkingDir);
        CopyAllFiles(fullTemplateDir, fullWorkingDir);
    }
    
    private string SanitizeDirectoryName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
    }
    
    public string OrganizeOutputFiles(string workingDir, string outputFileName, ApplicationData appData)
    {
        var fullWorkingDir = Path.Combine(_pathProvider.GetTemplatesPath(), workingDir);
        var sanitizedCompany = SanitizeDirectoryName(appData.CompanyName);
        var sanitizedPosition = SanitizeDirectoryName(appData.Position);
    
        var outputDir = Path.Combine(
            _pathProvider.GetApplicationsPath(), 
            $"{DateTime.Now:yyyy-MM-dd} - {sanitizedCompany}", 
            sanitizedPosition
        );
    
        Directory.CreateDirectory(outputDir);
        var sourcePdf = Path.Combine(fullWorkingDir, "main.pdf");
    
        if (!File.Exists(sourcePdf))
        {
            var texFiles = Directory.GetFiles(fullWorkingDir, "*.pdf");
            if (texFiles.Length > 0)
            {
                sourcePdf = texFiles[0];
            }
        }
    
        if (!File.Exists(sourcePdf))
        {
            throw new FileNotFoundException($"Could not find compiled PDF in {fullWorkingDir}");
        }

        // Create three versions
        var coverLetterOnlyPath = Path.Combine(outputDir, $"{outputFileName} - 0.pdf");
        File.Copy(sourcePdf, coverLetterOnlyPath, true);
    
        var resumePath = Path.Combine(_pathProvider.GetBasePath(), "Data", "Attachments", "Hussain, Arfaz - Resume.pdf");
        var transcriptPath = Path.Combine(_pathProvider.GetBasePath(), "Data", "Attachments", "Hussain, Arfaz - Transcript.pdf");
    
        if (File.Exists(resumePath))
        {
            var coverLetterWithResumePath = Path.Combine(outputDir, $"{outputFileName} - 1.pdf");
            CombinePdfs(coverLetterOnlyPath, resumePath, coverLetterWithResumePath);
        
            if (File.Exists(transcriptPath))
            {
                var fullApplicationPath = Path.Combine(outputDir, $"{outputFileName} - 2.pdf");
                CombinePdfs(coverLetterWithResumePath, transcriptPath, fullApplicationPath);
            }
        }
    
        return coverLetterOnlyPath;
    }
    
    private void CombinePdfs(string file1, string file2, string outputPath)
    {
        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(writer);
        using var firstDoc = new PdfDocument(new PdfReader(file1));
        firstDoc.CopyPagesTo(1, firstDoc.GetNumberOfPages(), pdf);
    
        using var secondDoc = new PdfDocument(new PdfReader(file2));
        secondDoc.CopyPagesTo(1, secondDoc.GetNumberOfPages(), pdf);
    }

    private void CopyAllFiles(string sourceDir, string targetDir)
    {
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        }
        
        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(directory);
            var newTargetDir = Path.Combine(targetDir, dirName);
            Directory.CreateDirectory(newTargetDir);
            CopyAllFiles(directory, newTargetDir);
        }
    }
}