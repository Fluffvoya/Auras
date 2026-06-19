using ASIS.Core.Models;
using ASIS.Core.Repositories;

namespace ASIS.Core.Services;

public class ArchiveService
{
    private readonly MetadataRepository _metadata;

    private readonly ArchiveConfigRepository _archiveConfig;

    public ArchiveService(MetadataRepository metadata, ArchiveConfigRepository archiveConfig)
    {
        _metadata = metadata;
        _archiveConfig = archiveConfig;
    }

    public DiffResult Diff()
    {
        var result = new DiffResult();
        var rootDir = _archiveConfig.ArchivePath ?? "";

        var metadataRecords = _metadata.GetAll();
        var diskFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Collect all files on disk (excluding metadata and config files)
        if (Directory.Exists(rootDir))
        {
            foreach (var file in Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(rootDir, file);
                // Normalize path separators to forward slash for consistent comparison
                relativePath = relativePath.Replace('\\', '/');
                // Skip metadata and config files
                if (relativePath.EndsWith("archive.json", StringComparison.OrdinalIgnoreCase) ||
                    relativePath.EndsWith("metadata.json", StringComparison.OrdinalIgnoreCase) ||
                    relativePath.EndsWith("tag_index.json", StringComparison.OrdinalIgnoreCase) ||
                    relativePath.EndsWith("hash_index.json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                diskFiles.Add(relativePath);
            }
        }

        // Find records in metadata but not on disk (orphaned metadata)
        foreach (var record in metadataRecords)
        {
            var normalizedPath = record.RelativePath.Replace('\\', '/');
            if (!diskFiles.Contains(normalizedPath))
            {
                result.OrphanedMetadata.Add(record);
            }
        }

        // Find files on disk but not in metadata (orphan disk files)
        var metadataPaths = new HashSet<string>(
            metadataRecords.Select(r => r.RelativePath.Replace('\\', '/')),
            StringComparer.OrdinalIgnoreCase);

        foreach (var diskPath in diskFiles)
        {
            if (!metadataPaths.Contains(diskPath))
            {
                result.OrphanedDiskFiles.Add(diskPath);
            }
        }

        return result;
    }
}

public class DiffResult
{
    public List<FileRecord> OrphanedMetadata { get; set; } = new();

    public List<string> OrphanedDiskFiles { get; set; } = new();
}