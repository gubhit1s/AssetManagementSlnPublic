using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AssetManagementApi.Models.Authentication;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Controllers;

[Authorize(Roles = "Administrator")]
[ApiController]
[Route("api/[controller]")]
public class PermissionController : ControllerBase {

    private readonly AssetContext _assetContext;
    private readonly IAdUser _adUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionController(AssetContext assetContext, IAdUser aduser, UserManager<ApplicationUser> userManager)
    {
        _assetContext = assetContext;
        _adUser = aduser;
        _userManager = userManager;
    }

    [HttpGet("permittedusers")]
    public async Task<ActionResult<List<UserDTO>>> GetAllPermittedUsersAsync()
    {
        return await _userManager.Users.Select(u => new UserDTO()
        {
            UserName = u.UserName,
            DisplayName = u.DisplayName,
            Email = u.Email
        }).ToListAsync();
    }

    [HttpPost("permittedusers")]
    public async Task<ActionResult> AddNewAdUserToStoreAsync(List<UserIdentity> users)
    {
        foreach (UserIdentity userAdded in users)
        {
            ApplicationUser? user = await _userManager.FindByNameAsync(userAdded.UserName);
            if (user is not null)
            {
                return BadRequest($"Account {userAdded.UserName} already exists in the system!");
            }

            string userEmail = _adUser.GetEmail(userAdded.UserName);
            string displayName = _adUser.GetDisplayName(userAdded.UserName);

            ApplicationUser adUser = new()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = userAdded.UserName,
                Email = userEmail,
                IsAdAccount = true,
                DisplayName = displayName
            };

            adUser.EmailConfirmed = true;
            adUser.LockoutEnabled = true;

            string role_Administrator = "Administrator";

            var result = await _userManager.CreateAsync(adUser);
            await _userManager.AddToRoleAsync(adUser, role_Administrator);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.ElementAt(0).Description);
            }
        }
        await _assetContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("permittedusers/revoke")]
    public async Task<ActionResult> RevokeUserAccessAsync(string userName)
    { 
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            return BadRequest("Cannot find proper user to delete");
        }

        if (userName.ToLower() == "admin")
        {
            return BadRequest("Cannot delete the admin user");
        }

        await _userManager.DeleteAsync(user);
        await _assetContext.SaveChangesAsync();
        return NoContent();
    }


}