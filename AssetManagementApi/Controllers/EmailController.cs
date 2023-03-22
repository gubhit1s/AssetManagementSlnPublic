using AssetManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Administrator")]
[Route("api/[controller]")]
[ApiController]
public class EmailController : ControllerBase
{

    private readonly AssetContext _assetContext;

    public EmailController(AssetContext assetContext)
    {
        _assetContext = assetContext;
    }

    [HttpGet]
    public async Task<ActionResult<EmailAdministrator>> GetEmailAdminAsync()
    {
        var emailInstance = await _assetContext.EmailAdministrators.FindAsync(2);

        if (emailInstance is null)
        {
            return NotFound("Can not find the proper admin email");
        }

        return emailInstance;
    }

    [HttpPut]
    public async Task<ActionResult> EditEmailAdminAsync(EmailAdministrator newEmail)
    {
        newEmail.Id = 2;
        _assetContext.Entry(newEmail).State = EntityState.Modified;
        try
        {
            await _assetContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }

        return NoContent();
    }

}
