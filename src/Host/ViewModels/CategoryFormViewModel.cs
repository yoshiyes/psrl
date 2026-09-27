using System.ComponentModel.DataAnnotations;
using Passerelle.Host.Validation;

namespace Passerelle.Host.ViewModels;

public sealed class CategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Field_Name")]
    [Required(ErrorMessage = "Validation_Name_Required")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Validation_Name_Length")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Field_Emoji")]
    [StringLength(32, ErrorMessage = "Validation_Emoji_Length")]
    [Emoji(ErrorMessage = "Validation_Emoji_Invalid")]
    public string? Emoji { get; set; }
}
