namespace easel.Core.Models;

public class ApplicationData
{
    public string Position { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanySuffix { get; init; } = string.Empty;
    public string Division { get; init; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Terms { get; set; } = string.Empty;
    public string UpTerm { get; set; } = string.Empty;
    public bool HasReference { get; set; }
    public string? ReferenceName { get; set; }
    public string? ReferenceTitle { get; set; }
    public string? ReferenceCity { get; set; }
    public string? ReferenceState { get; set; }

    public string GenerateFileName()
    {
        if (Position.Equals("TEST", StringComparison.OrdinalIgnoreCase))
        {
            return $"TEST-{DateTime.Now:yyyy-MM-dd-HHmm}";
        }

        // Format: Name - Application - Company - Position - Date
        return $"Hussain, Arfaz - {CompanyName} - {Position}"
            .Replace("/", "")
            .Replace("\\", "")
            .Replace(";", "");
    }
}
