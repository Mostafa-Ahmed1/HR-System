using System.ComponentModel.DataAnnotations;

namespace HR_System.ViewModels;

public sealed class EditUserViewModel
{
    public int UserId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    [Display(Name = "User Name")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Group")]
    public int? GroupId { get; set; }
}
