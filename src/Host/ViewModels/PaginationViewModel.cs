namespace Passerelle.Host.ViewModels;

public class PaginationViewModel
{
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public string FormId { get; init; } = "";
}