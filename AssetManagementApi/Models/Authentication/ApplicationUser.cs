using Microsoft.AspNetCore.Identity;

namespace AssetManagementApi.Models.Authentication;

public class ApplicationUser : IdentityUser
{
    public bool IsAdAccount { get; set; }

    public string? DisplayName { get; set; }
}