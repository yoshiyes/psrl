namespace Passerelle.Host.ViewModels;

public sealed class SortButtonViewModel
{
    public required string CurrentSort { get; init; }
    public required string SortKey { get; init; }
    public required string Label { get; init; }
    public required string FormId { get; init; }
}