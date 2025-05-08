using easel.Core.Models;
using easel.Infrastructure.Interface;
using Microsoft.Extensions.Configuration;

namespace easel.Infrastructure.Services;
public class BasePathProvider : IBasePathProvider
{
    private readonly IConfiguration _config;
    private readonly string _baseDirectory;
    
    public string GetFullName() => _config["PersonalInfo:FullName"] ?? "NAME";
    public string GetLocation() => _config["PersonalInfo:Location"] ?? "LOCATION";
    public string GetPhone() => _config["PersonalInfo:Phone"] ?? "PHONE";
    public string GetEmail() => _config["PersonalInfo:Email"] ?? "EMAIL";
    public string GetLinkedIn() => _config["PersonalInfo:LinkedIn"] ?? "LINKEDIN";
    public string GetGitHub() => _config["PersonalInfo:GitHub"] ?? "GITHUB";
    
    public string[] GetTermPeriods()
    {
        var section = _config.GetSection("TermPeriods");
        if (!section.Exists())
        {
            Console.WriteLine("Warning: No TermPeriods found in configuration, using default");
            return ["DEFAULT TERM PERIOD"]; 
        }

        try 
        {
            return section.Get<string[]>() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading TermPeriods: {ex.Message}");
            return ["FALLBACK TERM PERIOD"];
        }
    }
    
    public BasePathProvider(IConfiguration config)
    {
        _config = config;
        _baseDirectory = _config["Paths:RootDirectory"] 
                       ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        
    }

    public string GetBasePath() => _baseDirectory;
    public string GetTemplatesPath() => Path.Combine(_baseDirectory, _config["Paths:Templates"] ?? "Templates");
    public string GetApplicationsPath() => Path.Combine(_baseDirectory, _config["Paths:Applications"] ?? "Applications");
    public string GetDataPath() => Path.Combine(_baseDirectory, _config["Paths:Data"] ?? "Data");

    public void EnsureDirectoriesExist()
    {
        try
        {
            Directory.CreateDirectory(GetTemplatesPath());
            Directory.CreateDirectory(GetApplicationsPath());
            Directory.CreateDirectory(GetDataPath());
            Directory.CreateDirectory(Path.Combine(GetTemplatesPath(), "Temporary"));
            // Log success
            Console.WriteLine("Directories ensured.");
        }
        catch (Exception ex)
        {
            // Log error
            Console.Error.WriteLine($"Error ensuring directories exist: {ex.Message}");
            throw;
        }
    }
    
    public ApplicationData GetTestData()
    {
        var testSection = _config.GetSection("TestTemplate");
        return new ApplicationData
        {
            Position = testSection["Position"] ?? "TEST",
            CompanyName = testSection["CompanyName"] ?? "Test Company",
            CompanySuffix = testSection["CompanySuffix"] ?? "",
            Division = testSection["Division"] ?? "Test Division",
            City = testSection["City"] ?? "Test City",
            State = testSection["State"] ?? "Test State",
            Terms = testSection["Terms"] ?? "Test Term",
            UpTerm = testSection["UpTerm"] ?? "Test Term",
            HasReference = false
        };
    }
}
