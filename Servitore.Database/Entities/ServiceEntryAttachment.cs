using System;

namespace Servitore.Database.Entities;

public class ServiceEntryAttachment
{
    public int Id { get; set; }
    public int ServiceEntryId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string AttachmentType { get; set; } = string.Empty; // ProductPhoto, BeforeRepairPhoto, AfterRepairPhoto, Bill, Pdf, Other
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public ServiceEntry? ServiceEntry { get; set; }
}
