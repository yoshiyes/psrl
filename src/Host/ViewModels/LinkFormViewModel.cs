using System.ComponentModel.DataAnnotations;
using Passerelle.Domain.Entities;

namespace Passerelle.Host.ViewModels;

public sealed class LinkFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Field_Title")]
    [Required(ErrorMessage = "Validation_Title_Required")]
    [StringLength(160, MinimumLength = 2, ErrorMessage = "Validation_Title_Length")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Field_Url")]
    [Required(ErrorMessage = "Validation_Url_Required")]
    [Url(ErrorMessage = "Validation_Url_Invalid")]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Field_Description")]
    [Required(ErrorMessage = "Validation_Description_Required")]
    [StringLength(600, MinimumLength = 10, ErrorMessage = "Validation_Description_Length")]
    public string Description { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Validation_Categories_Min")]
    public List<Guid> SelectedCategoryIds { get; set; } = [];

    public IReadOnlyList<Category> AvailableCategories { get; set; } = [];
}
