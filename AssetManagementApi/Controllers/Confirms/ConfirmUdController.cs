using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Models;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Extensions;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Logics.TokenValidation;
using Hangfire;

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/device/unidentified/confirm")]
public class ConfirmUnidentifiedController : ControllerBase
{

    private readonly AssetContext _assetContext;
    private readonly IAdUser _adUser;
    private readonly TokenHandlerUd _tokenHandler;
    private readonly ILogger<ConfirmUnidentifiedController> _logger;
    private readonly IConfiguration _configuration;

    public ConfirmUnidentifiedController(AssetContext assetContext, IAdUser adUser, TokenHandlerUd tokenHandler,
        ILogger<ConfirmUnidentifiedController> logger, IConfiguration configuration)
    {
        _assetContext = assetContext;
        _adUser = adUser;
        _tokenHandler = tokenHandler;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> HandleTokenAndEmailForUdAsync(ConfirmationTokenUd ct)
    {
        if (ct.TransferTypeId == (int)TransferTypes.Withdrawal)
        {
            UserDeviceUd? ud = await _assetContext.UserDevicesUd.FirstOrDefaultAsync(ud => ud.DeviceTypeId == ct.DeviceTypeId);
            if (ud == null)
            {
                return BadRequest("Cannot find a corresponding device type of this user to withdraw");
            }
        }

        string domainName = $"https://{Request.Host.Value}";

        Guid token = await _tokenHandler.AddNewTokenToDbAsync(ct.DeviceTypeId, ct.UserName!, ct.TransferTypeId);

        try
        {

            await _tokenHandler.SendEmailConfirmationAsync(ct.DeviceTypeId, ct.UserName!, token, ct.TransferTypeId, domainName);
        }
        catch (Exception)
        {
            await _assetContext.DeleteTokensAsync(token);
            throw;
        }

        ConfirmationTokenDTO dataForRemind = new ConfirmationTokenDTO()
        {
            DeviceTypeId = ct.DeviceTypeId,
            UserName = ct.UserName,
            Token = token,
            TransferTypeId = ct.TransferTypeId,
            ExpiryDate = ct.ExpiryDate
        };

        int expiryInMinutes = Convert.ToInt32(_configuration["ConfirmationExpiryTimeInMinutes"]);
        ScheduledBackgroundJob bgjob = new()
        {
            JobId = BackgroundJob.Schedule<TokenHandlerUd>((t) => t.HandleExpiredTokenAsync(dataForRemind), TimeSpan.FromMinutes(expiryInMinutes)),
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
        ConfirmationTokenUd? ct = await _assetContext.ConfirmationTokensUd.FirstOrDefaultAsync(t => t.Token == token);
        if (ct is null)
        {
            return NotFound("Can not find this token");
        }
        return Ok();
    }
}