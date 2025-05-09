using System.Text;
using easel.Core.Interface;
using easel.Core.Models;
using easel.Infrastructure.Interface;

namespace easel.Core.Services;

public class TemplateVariableService(IFileSystemService fileSystem, IBasePathProvider pathProvider) : ITemplateService
{
    public async Task<string> GenerateDocument(ApplicationData data, string templateName)
    {
        const string workingDir = "Temporary";
        if (templateName.Equals("TEST", StringComparison.OrdinalIgnoreCase))
        {
            data = pathProvider.GetTestData();
            templateName = "Developer"; 
        }
        
        fileSystem.PrepareWorkingDirectory(workingDir, templateName);
        var mainFileName = $"{templateName.ToLower()}.tex";
        var variablesFilePath = Path.Combine(pathProvider.GetTemplatesPath(), workingDir, "data", "variables.tex");

        // Generate LaTeX content
        var sb = new StringBuilder();
        AppendPersonalInfo(sb);
        AppendPositionInfo(sb, data);
        AppendLetterStructure(sb, data);
        AppendParagraphs(sb, templateName);
        AppendSignature(sb);

        // Add formatting for TEST template
        if (data.Position.Equals("TEST", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(@"\usepackage{xcolor}");
            sb.AppendLine(@"\pagecolor{black}");
            sb.AppendLine(@"\color{white}");
        }
        await File.WriteAllTextAsync(variablesFilePath, sb.ToString());

        var mainTexPath = Path.Combine(pathProvider.GetTemplatesPath(), workingDir, "main.tex");
        var templateTexPath = Path.Combine(pathProvider.GetTemplatesPath(), workingDir, mainFileName);

        if (File.Exists(templateTexPath) && !File.Exists(mainTexPath))
        {
            File.Copy(templateTexPath, mainTexPath);
        }

        return workingDir;
    }

    private void AppendPersonalInfo(StringBuilder sb)
    {
        sb.AppendLine("% Personal Information");
        sb.AppendLine($@"\newcommand{{\FullName}}{{{pathProvider.GetFullName()}}}");
        sb.AppendLine($@"\newcommand{{\Location}}{{{pathProvider.GetLocation()}}}");
        sb.AppendLine($@"\newcommand{{\Phone}}{{{pathProvider.GetPhone()}}}");
        sb.AppendLine($@"\newcommand{{\Email}}{{{pathProvider.GetEmail()}}}");
        sb.AppendLine($@"\newcommand{{\LinkedIn}}{{{pathProvider.GetLinkedIn()}}}");
        sb.AppendLine($@"\newcommand{{\GitHub}}{{{pathProvider.GetGitHub()}}}");
        sb.AppendLine();
    }

    private void AppendPositionInfo(StringBuilder sb, ApplicationData data)
    {
        sb.AppendLine("% Position Information");
        sb.AppendLine($@"\newcommand{{\Position}}{{{data.Position}}}");
        sb.AppendLine($@"\newcommand{{\CompanyName}}{{{data.CompanyName}}}");
        sb.AppendLine($@"\newcommand{{\CompanyNameSuffix}}{{{data.CompanySuffix}}}");
        sb.AppendLine($@"\newcommand{{\Division}}{{{data.Division}}}");
        sb.AppendLine($@"\newcommand{{\LocationCity}}{{{data.City}}}");
        sb.AppendLine($@"\newcommand{{\LocationState}}{{{data.State}}}");
        sb.AppendLine($@"\newcommand{{\Terms}}{{{data.Terms}}}");
        sb.AppendLine($@"\newcommand{{\upTerm}}{{{data.UpTerm}}}");

        // Add reference information if present
        if (data.HasReference && !string.IsNullOrEmpty(data.ReferenceName))
        {
            sb.AppendLine($@"\newcommand{{\RecipientName}}{{{data.ReferenceName}}}");
            sb.AppendLine($@"\newcommand{{\RecipientTitle}}{{{data.ReferenceTitle ?? ""}}}");
        
            // Format company details with recipient info
            sb.AppendLine(@"\newcommand{\CompanyDetails}{");
            sb.AppendLine(@"    \textbf{\RecipientName} \\");
            sb.AppendLine(@"    \RecipientTitle \\");
            sb.AppendLine(@"    \Division, \textit{\CompanyName}\textit{ \CompanyNameSuffix} \\");
            sb.AppendLine(@"    \text{\LocationCity}, \text{\LocationState} \CompanyAddressSpacing");
            sb.AppendLine(@"}");
        }
        else
        {
            // Format company details without recipient info
            sb.AppendLine(@"\newcommand{\CompanyDetails}{");
            sb.AppendLine(@"    \textbf{\CompanyName}\textbf{ \CompanyNameSuffix}\CompanyDivisionSpacing");
            sb.AppendLine(@"    \text{\Division}\LocationSpacing");
            sb.AppendLine(@"    \text{\LocationCity}, \text{\LocationState} \CompanyAddressSpacing");
            sb.AppendLine(@"}");
        }
    
        sb.AppendLine();
    }

    private static void AppendLetterStructure(StringBuilder sb, ApplicationData data)
    {
        sb.AppendLine("% Letter structure texts");
        sb.AppendLine(@$"\newcommand{{\SubjectLine}}{{\underline{{\textbf{{Re: \Position\ Co-op Placement at \CompanyName}}}}}}");
        sb.AppendLine(@"\newcommand{\GreetingText}{Dear Hiring Manager:}");
        sb.AppendLine(@"\newcommand{\ClosingText}{Most Sincerely,}");
        sb.AppendLine();
    }

    private void AppendParagraphs(StringBuilder sb, string templateName)
    {
        var variablesDir = Path.Combine(pathProvider.GetTemplatesPath(), templateName, "variables");
        
        sb.AppendLine("% Paragraphs");
        sb.AppendLine(@"\newcommand{\IntroductionParagraph}{");
        sb.AppendLine(@"  I am writing to express my interest in the \textit{\Position} Co-op placement at {\CompanyName} for the upcoming {\upTerm} term. I am eager to apply and further develop my technical skills in a practical setting and contribute to real-world projects.");
        sb.AppendLine("}");
    
        // Read and append FirstParagraph
        var firstParagraphPath = Path.Combine(variablesDir, "first.txt");
        if (File.Exists(firstParagraphPath))
        {
            sb.AppendLine(@"\newcommand{\FirstParagraph}{");
            sb.AppendLine($"  {File.ReadAllText(firstParagraphPath).Trim()}");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine(@"\newcommand{\FirstParagraph}{");
            sb.AppendLine(@"No text files found for First Paragraph :<");
            sb.AppendLine("}");
        }
    
        // Read and append SecondParagraph
        var secondParagraphPath = Path.Combine(variablesDir, "second.txt");
        if (File.Exists(secondParagraphPath))
        {
            sb.AppendLine(@"\newcommand{\SecondParagraph}{");
            sb.AppendLine($"  {File.ReadAllText(secondParagraphPath).Trim()}");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine(@"\newcommand{\SecondParagraph}{");
            sb.AppendLine(@"No text files found for Second Paragraph :<");
            sb.AppendLine("}");
        }
    
        // Read and append ThirdParagraph
        var thirdParagraphPath = Path.Combine(variablesDir, "third.txt");
        if (File.Exists(thirdParagraphPath))
        {
            sb.AppendLine(@"\newcommand{\ThirdParagraph}{");
            sb.AppendLine($"  {File.ReadAllText(thirdParagraphPath).Trim()}");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine(@"\newcommand{\ThirdParagraph}{");
            sb.AppendLine(@"No text files found for Third Paragraph :<");
            sb.AppendLine("}");
        }
    
        sb.AppendLine(@"\newcommand{\FourthParagraph}{");
        sb.AppendLine(@"  Through these combined experiences, I have gained a good understanding of software development and testing practices. I am currently seeking a 4- or 8-month co-op work term. I really appreciate the time taken to review my application and look forward to speaking with the team further about this role.");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendSignature(StringBuilder sb)
    {
        sb.AppendLine(@"\newcommand{\SignatureTitle}{");
        sb.AppendLine(@"  B.Eng. Software Engineering Undergraduate,\\Faculty of Engineering, University of Victoria");
        sb.AppendLine("}");
    }

    public Task<string> CompileToPdf(string workingDirectory, string outputFileName, ApplicationData appData)
    {
        throw new NotImplementedException();
    }
}
