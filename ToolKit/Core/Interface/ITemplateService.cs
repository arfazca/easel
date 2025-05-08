using easel.Core.Models;

namespace easel.Core.Interface;

public interface ITemplateService
{
    Task<string> GenerateDocument(ApplicationData data, string templateName);
    Task<string> CompileToPdf(string workingDirectory, string outputFileName, ApplicationData appData);
}
