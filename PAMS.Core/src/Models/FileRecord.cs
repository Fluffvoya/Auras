namespace PAMS.Core.Models;

public class FileRecord
{
    public Guid Id { get; set; }

    public string Hash { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    
    public string PrimaryTag { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    public DateTime CreatedTime { get; set; }

}