using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models.Authentication;

public class UserForAuthentication
{

    [Required(ErrorMessage = "User name is required")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = null!;
}