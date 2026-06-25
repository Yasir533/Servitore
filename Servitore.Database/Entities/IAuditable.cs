using System;

namespace Servitore.Database.Entities;

public interface IAuditable
{
    string? CreatedBy { get; set; }
    DateTime CreatedDate { get; set; }
    string? ModifiedBy { get; set; }
    DateTime? ModifiedDate { get; set; }
}
