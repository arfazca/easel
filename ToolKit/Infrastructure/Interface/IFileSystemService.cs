using easel.Core.Models;

namespace easel.Infrastructure.Interface;


public interface IFileSystemService
{
    void PrepareWorkingDirectory(string workingDir, string templateDir);
    string OrganizeOutputFiles(string workingDir, string outputFileName, ApplicationData appData);
}