using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Passerelle.Domain.Entities;

namespace Passerelle.Host.ViewModels;

public sealed class GalleryFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Gallery_Field_File")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Field_Title")]
    [StringLength(160, ErrorMessage = "Validation_Title_Length")]
    public string? Title { get; set; }

    [Display(Name = "Field_Description")]
    [StringLength(1200, ErrorMessage = "Validation_GalleryDescription_Length")]
    public string? Description { get; set; }

    [Display(Name = "Gallery_Field_Type")]
    public GalleryItemType Type { get; set; } = GalleryItemType.Drawing;

    [Display(Name = "Gallery_Field_LocationMode")]
    public GalleryLocationMode LocationMode { get; set; } = GalleryLocationMode.Original;

    [Display(Name = "Gallery_Field_LocationName")]
    [StringLength(160)]
    public string? LocationName { get; set; }

    [Display(Name = "Gallery_Field_Latitude")]
    [Range(-90d, 90d, ErrorMessage = "Validation_Latitude_Range")]
    public double? Latitude { get; set; }

    [Display(Name = "Gallery_Field_Longitude")]
    [Range(-180d, 180d, ErrorMessage = "Validation_Longitude_Range")]
    public double? Longitude { get; set; }

    public List<Guid> SelectedTagIds { get; set; } = [];

    [Display(Name = "Gallery_Field_NewTags")]
    [StringLength(400)]
    public string? NewTags { get; set; }

    public IReadOnlyList<ImageTag> AvailableTags { get; set; } = [];
}
