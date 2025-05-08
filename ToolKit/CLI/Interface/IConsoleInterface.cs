using easel.Core.Models;

namespace easel.CLI.Interface;

public interface IConsoleInterfaceService
{
    Task<ApplicationData> CollectApplicationData(string[] termPeriods);
    Task<string> SelectTemplate();
}
