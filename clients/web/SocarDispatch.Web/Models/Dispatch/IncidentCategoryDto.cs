using System.ComponentModel.DataAnnotations;

namespace SocarDispatch.Web.Models.Dispatch;

public class IncidentCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateIncidentCategoryRequestDto
{
    [Required(ErrorMessage = "Category code is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 50 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;
}

public class UpdateIncidentCategoryRequestDto
{
    [Required(ErrorMessage = "Category code is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 50 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
