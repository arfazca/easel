using easel.Core.Models;

namespace easel.CLI.Interface;

public interface IConsoleInterfaceService
{
    Task<ApplicationData> CollectApplicationData(string[] termPeriods);
    Task<string> SelectTemplate();
    ApplicationData CreateApplicationDataFromArgs(
        string position, string companyName, string companySuffix, 
        string division, string city, string state, string terms, string[] termPeriods);
}
