using System;

namespace Servitore.Database.Entities;

public class AssetDocument
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public Asset? Asset { get; set; }
}
