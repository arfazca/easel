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
        var sanitizedCompanyName = SanitizeDirectoryName(appData.CompanyName);

        // Create timestamp prefix in format yymmddHHmm 
        var timestampPrefix = DateTime.Now.ToString("yyMMddHHmm");
        
        // Create directory name with timestamp, company and position
        var outputDirName = $"{timestampPrefix} - {sanitizedCompanyName}";
        var outputDir = Path.Combine(_pathProvider.GetApplicationsPath(), outputDirName);
        
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

        // Paths to attachment files
        var resumePath = Path.Combine(_pathProvider.GetBasePath(), "Data", "Attachments", "Hussain, Arfaz - Resume.pdf");
        var transcriptPath = Path.Combine(_pathProvider.GetBasePath(), "Data", "Attachments", "Hussain, Arfaz - Transcript.pdf");
        var recommendationsPath = Path.Combine(_pathProvider.GetBasePath(), "Data", "Attachments", "Hussain, Arfaz - Recommendations.pdf");

        // 0 - Cover letter only
        var coverLetterOnlyPath = Path.Combine(outputDir, $"{outputFileName} - 0.pdf");
        File.Copy(sourcePdf, coverLetterOnlyPath, true);

        // 1 - Cover letter + Resume
        if (File.Exists(resumePath))
        {
            var coverWithResumePath = Path.Combine(outputDir, $"{outputFileName} - 1.pdf");
            CombinePdfs(sourcePdf, resumePath, coverWithResumePath);
        }

        // 2 - Cover letter + Resume + Recommendations
        if (File.Exists(resumePath) && File.Exists(recommendationsPath))
        {
            var tempPath = Path.Combine(outputDir, "temp.pdf");
            CombinePdfs(sourcePdf, resumePath, tempPath);
            var coverWithResumeAndRecsPath = Path.Combine(outputDir, $"{outputFileName} - 2.pdf");
            CombinePdfs(tempPath, recommendationsPath, coverWithResumeAndRecsPath);
            File.Delete(tempPath);
        }

        // 3 - Cover letter + Resume + Recommendations + Transcript
        if (File.Exists(resumePath) && File.Exists(recommendationsPath) && File.Exists(transcriptPath))
        {
            var tempPath1 = Path.Combine(outputDir, "temp1.pdf");
            var tempPath2 = Path.Combine(outputDir, "temp2.pdf");
            CombinePdfs(sourcePdf, resumePath, tempPath1);
            CombinePdfs(tempPath1, recommendationsPath, tempPath2);
            var fullApplicationPath = Path.Combine(outputDir, $"{outputFileName} - 3.pdf");
            CombinePdfs(tempPath2, transcriptPath, fullApplicationPath);
            File.Delete(tempPath1);
            File.Delete(tempPath2);
        }

        // 4 - Resume 
        if (File.Exists(resumePath))
        {
            var resumeOnlyPath = Path.Combine(outputDir, $"{outputFileName} - 4.pdf");
            File.Copy(resumePath, resumeOnlyPath, true);
        }

        // 5 - Transcript 
        if (File.Exists(transcriptPath))
        {
            var transcriptOnlyPath = Path.Combine(outputDir, $"{outputFileName} - 5.pdf");
            File.Copy(transcriptPath, transcriptOnlyPath, true);
        }

        // 6 - Recommendations 
        if (File.Exists(recommendationsPath))
        {
            var recommendationsOnlyPath = Path.Combine(outputDir, $"{outputFileName} - 6.pdf");
            File.Copy(recommendationsPath, recommendationsOnlyPath, true);
        }

        ArchivePastApplications();

        return coverLetterOnlyPath;
    }
    
    private void ArchivePastApplications()
    {
        var appsDir = _pathProvider.GetApplicationsPath();
        var archiveDir = Path.Combine(appsDir, "PAST");
        Directory.CreateDirectory(archiveDir);

        // Get all directories except "PAST"
        var allDirs = Directory.GetDirectories(appsDir)
            .Where(d => !Path.GetFileName(d).Equals("PAST", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Keep track of today's directories to not archive them
        var todayPrefix = DateTime.Now.ToString("yyMMdd");
        Console.WriteLine($"Today's prefix: {todayPrefix}");

        foreach (var dir in allDirs)
        {
            var dirName = Path.GetFileName(dir);
            Console.WriteLine($"Processing directory: {dirName}");
            
            // Skip directories created today
            if (IsTodayDirectory(dirName, todayPrefix))
            {
                Console.WriteLine($"Skipping today's directory: {dirName}");
                continue;
            }
            
            // Process archivable directories
            ProcessArchivableDirectory(dir, dirName, archiveDir);
        }
    }

    private bool IsTodayDirectory(string dirName, string todayPrefix)
    {
        return dirName.Length >= 6 && dirName.Substring(0, 6) == todayPrefix;
    }

    private void ProcessArchivableDirectory(string dirPath, string dirName, string archiveDir)
    {
        // Parse timestamp from directory name (format: yymmddHHmm - Company)
        if (dirName.Length >= 10)
        {
            try
            {
                // Extract the timestamp portion (first 10 characters)
                var timestampPart = dirName.Substring(0, 10);
            
                // Verify it's numeric and in the expected format
                if (!long.TryParse(timestampPart, out _))
                {
                    Console.WriteLine($"Directory name does not start with numeric timestamp: {dirName}");
                    return;
                }

                Console.WriteLine($"Found timestamp: {timestampPart}");
            
                // Find cover letter files
                var coverLetters = FindCoverLetterFiles(dirPath);
                var coverLettersList = coverLetters.ToList();
            
                if (coverLettersList.Count > 0)
                {
                    foreach (var coverLetter in coverLettersList)
                    {
                        ArchiveCoverLetter(coverLetter, dirName, timestampPart, archiveDir);
                    }
                }
                else
                {
                    Console.WriteLine($"No cover letters found in {dirName}");
                }

                // Delete the original directory after archiving
                Directory.Delete(dirPath, recursive: true);
                Console.WriteLine($"Deleted directory: {dirName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error archiving {dirName}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Directory name too short: {dirName}");
        }
    }

    private IEnumerable<string> FindCoverLetterFiles(string dirPath)
    {
        // Find all PDF files in the directory that end with "- 0.pdf"
        return Directory.GetFiles(dirPath, "*- 0.pdf");
    }

    private void ArchiveCoverLetter(string coverLetterPath, string dirName, string timestamp, string archiveDir)
    {
        try
        {
            // Parse company from directory name (format: "yymmddHHmm - Company")
            var dirParts = dirName.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            string company = "Unknown Company";
        
            if (dirParts.Length >= 2)
            {
                company = dirParts[1];
            }
        
            // Get file name without extension
            var fileName = Path.GetFileNameWithoutExtension(coverLetterPath);
            Console.WriteLine($"Processing cover letter: {fileName}");
        
            // Extract position from filename
            var position = ExtractPositionFromFilename(fileName, company);
        
            // Parse the timestamp to a DateTime
            if (DateTime.TryParseExact(timestamp, "yyMMddHHmm", null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                // Format: "dd MMMM yyyy HH.mm - Company - Position.pdf" (24-hour format with period)
                var newFileName = $"{date:dd MMMM yyyy HH.mm} - {company} - {position}.pdf";
                var destPath = Path.Combine(archiveDir, newFileName);
            
                // Ensure the destination file name is valid
                destPath = EnsureValidFilePath(destPath);
            
                // Copy the cover letter to archive
                File.Copy(coverLetterPath, destPath, overwrite: true);
                Console.WriteLine($"Archived: {Path.GetFileName(coverLetterPath)} to {Path.GetFileName(destPath)}");
            }
            else
            {
                Console.WriteLine($"Failed to parse timestamp: {timestamp}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ArchiveCoverLetter: {ex.Message}");
        }
    }
    
    private string EnsureValidFilePath(string path)
    {
        // Replace invalid characters in the file path
        var invalidChars = Path.GetInvalidFileNameChars();
        var fileName = Path.GetFileName(path);
        var directory = Path.GetDirectoryName(path);
    
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "unnamed.pdf";
        }
    
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
    
        // Ensure directory is not null
        if (string.IsNullOrEmpty(directory))
        {
            return fileName;
        }
    
        return Path.Combine(directory, fileName);
    }
    
    private string ExtractPositionFromFilename(string fileName, string company)
    {
        string position = "Unknown Position";
    
        try
        {
            // Example format: "Hussain, Arfaz - Nokia - Software Test Automation - 0"
            var fileParts = fileName.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
        
            // Find the company part in the filename
            int companyIndex = -1;
            for (int i = 0; i < fileParts.Length; i++)
            {
                if (fileParts[i].Equals(company, StringComparison.OrdinalIgnoreCase))
                {
                    companyIndex = i;
                    break;
                }
            }
        
            if (companyIndex >= 0 && companyIndex + 1 < fileParts.Length - 1)
            {
                // Position is between company and the ending number
                position = fileParts[companyIndex + 1];
            
                // If there are more parts, join them (for positions with hyphens)
                if (companyIndex + 2 < fileParts.Length - 1)
                {
                    var positionParts = new List<string> { position };
                    for (int i = companyIndex + 2; i < fileParts.Length - 1; i++)
                    {
                        positionParts.Add(fileParts[i]);
                    }
                    position = string.Join(" - ", positionParts);
                }
            }
        
            Console.WriteLine($"Extracted position: {position}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting position: {ex.Message}");
        }
    
        return position;
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