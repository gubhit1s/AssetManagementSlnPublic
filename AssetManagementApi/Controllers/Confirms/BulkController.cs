using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Extensions;
using AssetManagementApi.Logics.TokenValidation;
using Hangfire;

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BulkController : ControllerBase
{

    private readonly AssetContext _assetContext;
    private readonly IBulkTokenHandler _bulkTokenHandler;
    private readonly IConfiguration _configuration;

    public BulkController(AssetContext assetContext, IBulkTokenHandler bulkTokenHandler, IConfiguration configuration)
    {
        _assetContext = assetContext;
        _bulkTokenHandler = bulkTokenHandler;
        _configuration = configuration;
    }

    [HttpPost("allcurrentdevices")]
    public async Task<ActionResult<CurrentDevicesDTO>> GetDevicesOfUserByTypesAsync(CurrentUserCartDTO cart)
    {
        IQueryable<DeviceDTO> devicesOfUser = _assetContext.UserDevices.Include(ud => ud.Device).Where(
            ud => cart.DeviceTypeIds.Contains(ud.Device.DeviceTypeId) && ud.UserName == cart.UserName && ud.UserOrderId == 0)
            .Select(d => new DeviceDTO()
            {
                ServiceTag = d.Device!.ServiceTag,
                DeviceName = d.Device!.DeviceName,
                DeviceTypeId = d.Device!.DeviceTypeId,
                DeviceTypeName = d.Device!.DeviceType!.Name
            }); 

        IQueryable<DeviceUnidentifiedDTO> devicesUdOfUser = _assetContext.UserDevicesUd.Include(ud => ud.DeviceType).Where(udu =>
            cart.DeviceTypeIds.Contains(udu.DeviceTypeId) && cart.UserName == udu.UserName).Select(d => new DeviceUnidentifiedDTO()
            {
                DeviceTypeId = d.DeviceTypeId,
                DeviceTypeName = d.DeviceType!.Name
            });

        CurrentDevicesDTO allCurrentDevices = new CurrentDevicesDTO()
        {
            CurrentDevicesIdentified = await devicesOfUser.ToListAsync(),
            CurrentDevicesUnidentified = await devicesUdOfUser.ToListAsync()
        };

        return allCurrentDevices;
    }

    [HttpPost]
    public async Task<IActionResult> HandleToken(List<CartDTO> carts)
    {
        await AddTokenAndSendEmailAsync(carts);
        return Accepted();
    }

    [HttpPost("confirm/verify")]
    public async Task<IActionResult> VerifyUserTokenAsync(ConfirmationToken ct)
    {
        bool verifySuccess = await _bulkTokenHandler.ValidateTokenAsync(ct.Token, ct.UserConfirmed, ct.DeclinedReason);
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

    [HttpGet("confirm")]
    public async Task<IActionResult> VerifyTokenExists(Guid token)
    {
        ConfirmationToken? ct = await _assetContext.ConfirmationTokens.FirstOrDefaultAsync(t => t.BatchId == token);
        ConfirmationTokenUd? ctUd = await _assetContext.ConfirmationTokensUd.FirstOrDefaultAsync(t => t.BatchId == token);
        if (ct is null && ctUd is null)
        {
            return NotFound("Can not find this token");
        }
        return Ok();
    }

    [HttpGet("withdraw")]
    public async Task<IActionResult> WithdrawAllDevicesAsync(string userName)
    {
        var userDevices = _assetContext.UserDevices.Include(ud => ud.Device).Where(ud => ud.UserName!.ToLower() == userName.ToLower() && ud.UserOrderId == 0);
        var userDeviceUds = _assetContext.UserDevicesUd.Where(udu => udu.UserName!.ToLower() == userName.ToLower());

        //Get All devices that user is using.
        List<CartDTO> carts = new List<CartDTO>();
        foreach (UserDevice ud in userDevices)
        {
            CartDTO cart = new CartDTO()
            {
                DeviceId = ud.DeviceId,
                DeviceTypeId = ud.Device.DeviceTypeId,
                TransferTypeId = (int)TransferTypes.Withdrawal,
                UserName = userName
            };
            carts.Add(cart);
        }

        foreach (UserDeviceUd udu in userDeviceUds)
        {
            CartDTO cart = new CartDTO()
            {
                DeviceTypeId = udu.DeviceTypeId,
                TransferTypeId = (int)TransferTypes.Withdrawal,
                UserName = userName
            };
            carts.Add(cart);
        }
        await AddTokenAndSendEmailAsync(carts);
        return Accepted();
    }

    private async Task AddTokenAndSendEmailAsync(List<CartDTO> carts)
    {
        Guid batchId = await _bulkTokenHandler.AddNewTokenToDbAsync(carts);
        string domainName = $"https://{Request.Host.Value}";

        try
        {
            await _bulkTokenHandler.SendEmailConfirmationAsync(carts, batchId, domainName);
        }
        catch (Exception)
        {
            await _assetContext.DeleteTokensAsync(batchId);
            throw;
        }

        int expiryInMinutes = Convert.ToInt32(_configuration["ConfirmationExpiryTimeInMinutes"]);

        ScheduledBackgroundJob bgJob = new()
        {
            JobId = BackgroundJob.Schedule<BulkTokenHandler>((b) => b.HandleExpiredTokenAsync(carts, batchId), TimeSpan.FromMinutes(expiryInMinutes)),
            Token = batchId
        };

        await _assetContext.ScheduledBackgroundJobs.AddAsync(bgJob);
        await _assetContext.SaveChangesAsync();
    }

}