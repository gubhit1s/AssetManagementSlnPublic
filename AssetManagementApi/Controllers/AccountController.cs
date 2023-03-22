using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AssetManagementApi.Models;
using AssetManagementApi.Models.Authentication;
using AssetManagementApi.Logics.JwtHandler;
using AssetManagementApi.Models.AdUserGeneration;
using System.IdentityModel.Tokens.Jwt;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace AssetManagementApi.Controllers;

#pragma warning disable CA1416 // Validate platform compatibility

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly AssetContext _context;
    private readonly JwtHandler _jwtHandler;
    private readonly IAdUser _adUser;

    public AccountController(UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IWebHostEnvironment env,
        AssetContext context,
        JwtHandler jwtHandler,
        SignInManager<ApplicationUser> signInManager,
        IAdUser adUser)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _env = env;
        _context = context;
        _jwtHandler = jwtHandler;
        _signInManager = signInManager;
        _adUser = adUser;
    }

    /// <summary>
    /// This method is used to create administor accounts only, comment out if not used.
    /// </summary>
    /// <returns></returns>
    /*
    [HttpGet]
    public async Task<IActionResult> CreateDefaultUsers()
    {

        string role_Administrator = "Administrator";

        if (await _roleManager.FindByNameAsync(role_Administrator) == null)
        {
            await _roleManager.CreateAsync(new IdentityRole(role_Administrator));
        }
        List<ApplicationUser> addedAccounts = new();

        string adminUserName = "admin";
        string adminEmail = "admin@email.com";
        if (await _userManager.FindByNameAsync(adminUserName) == null)
        {

            ApplicationUser admin = new ApplicationUser()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = adminUserName,
                Email = adminEmail
            };

            await _userManager.CreateAsync(admin, _configuration["DefaultPasswords:Administrator"]!);

            await _userManager.AddToRoleAsync(admin, role_Administrator);

            admin.EmailConfirmed = true;
            admin.LockoutEnabled = false;
            addedAccounts.Add(admin);

        }

        if (addedAccounts.Count > 0) await _context.SaveChangesAsync();

        return new JsonResult(new  //Anonymous type
        {
            Count = addedAccounts.Count,
            Users = addedAccounts
        });
    }*/

    [HttpPost("Login")]
    public async Task<IActionResult> Login(UserForAuthentication loginRequest)
    {
        ApplicationUser? user = await _userManager.FindByNameAsync(loginRequest.UserName);
        if (user == null)
        {
            return BadRequest("Invalid user name or password");
        }

        Microsoft.AspNetCore.Identity.SignInResult? loginResult;

        if (user.IsAdAccount == true)
        {
           loginResult = await AdSignInAsync(loginRequest);
        }
        else
        {
            loginResult = await _signInManager.PasswordSignInAsync(user, loginRequest.Password, true, true);
        }


        if (loginResult == Microsoft.AspNetCore.Identity.SignInResult.Success)
        {
            JwtSecurityToken secToken = await _jwtHandler.GetTokenAsync(user);
            string jwt = new JwtSecurityTokenHandler().WriteToken(secToken);
            return Ok(new LoginResult()
            {
                Success = true,
                Message = "Login successful",
                Token = jwt
            });
        }

        if (loginResult == Microsoft.AspNetCore.Identity.SignInResult.LockedOut)
        {
            return BadRequest("Too many attempts, this account is temporarily locked out!");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return BadRequest("Invalid User Name or Password");
        }
    }

    private async Task<Microsoft.AspNetCore.Identity.SignInResult> AdSignInAsync(UserForAuthentication loginRequest)
    {
        Microsoft.AspNetCore.Identity.SignInResult result = new();
        var user = await _userManager.FindByNameAsync(loginRequest.UserName);
        if (user == null)
        {
            throw new ArgumentException("Cannot find user to login");
        }

        bool adAuthenticated = false;
        using (var pc = new PrincipalContext(ContextType.Domain, "tmf-group.com", _configuration["ActiveDirectoryPath"]))
        {
            
            if (await _userManager.IsLockedOutAsync(user))
            {
                return Microsoft.AspNetCore.Identity.SignInResult.LockedOut;
            }

            adAuthenticated = pc.ValidateCredentials(loginRequest.UserName, loginRequest.Password);
            
            if (adAuthenticated == true)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                return Microsoft.AspNetCore.Identity.SignInResult.Success;
            }
            else
            {
                await _userManager.AccessFailedAsync(user);
                return Microsoft.AspNetCore.Identity.SignInResult.Failed;
            }
        }

    }


}