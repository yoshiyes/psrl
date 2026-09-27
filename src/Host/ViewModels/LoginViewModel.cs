using System.ComponentModel.DataAnnotations;

namespace Passerelle.Host.ViewModels;

public sealed class LoginViewModel
{
    [Display(Name = "Field_Email")]
    [Required(ErrorMessage = "Validation_Email_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email_Invalid")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Field_Password")]
    [Required(ErrorMessage = "Validation_Password_Required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Field_RememberMe")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
