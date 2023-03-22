using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models.Authentication;

public class UserIdentity
{

    [Required(ErrorMessage = "User Name is required")]
    public string UserName { get; set; } = null!;
}