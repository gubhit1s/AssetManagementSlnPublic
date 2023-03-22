using AssetManagementApi.Models;
using AssetManagementApi.Models.AdUserGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AssetManagementApi.Models.DTO;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Administrator")]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    //Use dependency injection, passing the interface type as a parameter in a constructor
    private readonly IAdUser _aduser;
    private readonly ILogger<UsersController> _logger;

    private readonly AssetContext _assetContext;

    public UsersController(IAdUser adUser, ILogger<UsersController> logger, AssetContext assetContext)
    {
        _aduser = adUser;
        _logger = logger;
        _assetContext = assetContext;
    }

    [HttpGet]
    public ActionResult<List<User>> GetUsers()
    {
        List<User> adUsers = _aduser.Users.ToList();
        _logger.LogInformation($"Successfully retrieved Ad Users. Total number of users is {adUsers.Count()}");
        return adUsers;
    }

    [HttpGet("{userName}")]
    public ActionResult<User> GetSpecificUser(string userName)
    {
        User? user = _aduser.Users.FirstOrDefault(u => u.UserName!.ToLower() == userName.ToLower());
        if (user is null)
        {
            return BadRequest("Cannot find the user");
        }
        return user;
    }

    [HttpGet("devices/{userName}")]
    public async Task<ActionResult<User>> GetDevicesOfSelectedUser(string userName)
    {
        User? user = _aduser.Users.FirstOrDefault(u => u.UserName?.ToLower() == userName.ToLower());
        if (user is null)
        {
            return BadRequest("Can not find the proper user to process");
        }

        List<DeviceDTO>? devicesOfUser = await _assetContext.UserDevices.AsNoTracking()
            .Include(ud => ud.Device).ThenInclude(d => d.DeviceType)
            .Include(ud => ud.Device).ThenInclude(d => d.DeviceStatus)
            .Where(ud => ud.UserName == user.UserName && ud.UserOrderId == 0).Select(ud => new DeviceDTO()
            {
                Id = ud.DeviceId,
                ServiceTag = ud.Device.ServiceTag,
                DeviceName = ud.Device.DeviceName,
                DeviceModel = ud.Device.DeviceModel,
                PONumber = ud.Device.PONumber,
                DeviceStatusId = ud.Device.DeviceStatusId,
                DeviceStatusName = ud.Device.DeviceStatus!.StatusName,
                DeviceTypeId = ud.Device.DeviceTypeId,
                DeviceTypeName = ud.Device.DeviceType!.Name,
            }).ToListAsync();

        List<DeviceType>? devicesUdOfUser = await _assetContext.UserDevicesUd
            .Where(udu => udu.UserName == userName).Select(udu => udu.DeviceType).ToListAsync();

        if (devicesOfUser is null && devicesUdOfUser is null)
        {
            return NoContent();
        }

        user.Devices = devicesOfUser;
        user.DevicesUnidentified = devicesUdOfUser;

        return user;
    }

}