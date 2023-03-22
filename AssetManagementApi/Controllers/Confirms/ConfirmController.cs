using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Logics.TokenValidation;
using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Extensions;
using Hangfire;

namespace AssetManagementApi.Controllers;

[Route("api/device/[controller]")]
[ApiController]
public class ConfirmController : ControllerBase
{

    private readonly AssetContext _assetContext;
    private readonly IEmailHandler _emailHandler;
    private readonly TokenHandler _tokenHandler;
    private readonly ILogger<ConfirmController> _logger;
    private readonly IAdUser _adUser;
    private readonly IConfiguration _configuration;

    public ConfirmController(
        AssetContext context,
        IEmailHandler emailHandler,
        TokenHandler tokenHandler,
        IAdUser adUser,
        ILogger<ConfirmController> logger,
        IConfiguration configuration)
    {
        _assetContext = context;
        _emailHandler = emailHandler;
        _tokenHandler = tokenHandler;
        _adUser = adUser;
        _logger = logger;
        _configuration = configuration;

    }

    [HttpPost]
    public async Task<IActionResult> HandleTokenAndEmailAsync(ConfirmationToken ct)
    {

        string domainName = $"https://{Request.Host.Value}";

        Guid token = await _tokenHandler.AddNewTokenToDbAsync(ct.DeviceId, ct.UserName!, ct.TransferTypeId);

        try
        {
            await _tokenHandler.SendEmailConfirmationAsync(ct.DeviceId, ct.UserName!, token, ct.TransferTypeId, domainName);
        } 
        catch (Exception)
        {
            await _assetContext.DeleteTokensAsync(token);
            throw;
        }

        ConfirmationTokenDTO dataForRemind = new ConfirmationTokenDTO()
        {
            DeviceId = ct.DeviceId,
            UserName = ct.UserName,
            Token = token,
            TransferTypeId = ct.TransferTypeId,
            ExpiryDate = ct.ExpiryDate
        };


        int expiryInMinutes = Convert.ToInt32(_configuration["ConfirmationExpiryTimeInMinutes"]);

        //Store a job to handle when a user did not confirm, notifying the expiration of the token.
        ScheduledBackgroundJob bgjob = new()
        {
            JobId = BackgroundJob.Schedule<TokenHandler>((tokenHandler) => tokenHandler.HandleExpiredTokenAsync(dataForRemind), TimeSpan.FromMinutes(expiryInMinutes)),
            Token = token
        };

        await _assetContext.ScheduledBackgroundJobs.AddAsync(bgjob);
        
        await _assetContext.SaveChangesAsync();
        return Accepted();
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyUserTokenAsync(ConfirmationToken ct)
    {
        bool verifySuccess = await _tokenHandler.ValidateTokenAsync(ct.Token, ct.UserConfirmed, ct.DeclinedReason);
        if (verifySuccess)
        {
            BackgroundJob.Enqueue(() => Console.WriteLine("Verification successful."));

            return Ok();
        }
        else
        {
            return BadRequest("This token has been expired or is invalid");
        }
    }

    [HttpGet]
    public async Task<IActionResult> VerifyTokenExists(Guid token)
    {
        ConfirmationToken? ct = await _assetContext.ConfirmationTokens.FirstOrDefaultAsync(t => t.Token == token);
        if (ct is null)
        {
            return NotFound("Can not find this token");
        }
        return Ok();
    }
}

