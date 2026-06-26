using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class SearchResultDto
{
    public List<SearchItemDto> Customers { get; set; } = new();
    public List<SearchItemDto> Products { get; set; } = new();
    public List<SearchItemDto> ServiceEntries { get; set; } = new();
    public List<SearchItemDto> Employees { get; set; } = new();
}

public class SearchItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}
