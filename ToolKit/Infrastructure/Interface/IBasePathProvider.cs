using easel.Core.Models;

namespace easel.Infrastructure.Interface;

public interface IBasePathProvider
{
    string GetBasePath();
    string GetFullName();
    string GetLocation();
    string GetPhone();
    string GetEmail();
    string GetLinkedIn();
    string GetGitHub();
    string GetTemplatesPath();
    string GetApplicationsPath();
    string[] GetTermPeriods();
    string GetDataPath();
    void EnsureDirectoriesExist();
    ApplicationData GetTestData();
}
