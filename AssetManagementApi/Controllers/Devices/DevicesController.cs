using AssetManagementApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Extensions;
using AssetManagementApi.Logics.UserOrderHandler;
using AssetManagementApi.Logics.TransferHandler.Reassignation;
using AssetManagementApi.Logics.TransferHandler.Decommission;
using AssetManagementApi.Models.AdUserGeneration;

namespace AssetManagementApi.Controllers;

[Authorize(Roles = "Administrator")]
[Route("api/[controller]")]
[ApiController]
public class DevicesController : ControllerBase
{

    private readonly AssetContext _context;
    private readonly IUserOrderHandler _userOrder;
    private readonly IReassignationHandler _reassignationHandler;
    private readonly IDecommissionHandler _decommissionHandler;
    private readonly IDecommissionPrepHandler _decommissionPrepHandler;
    private readonly IAdUser _adUser;


    public DevicesController(
        AssetContext context, 
        IUserOrderHandler userOrder, 
        IReassignationHandler reassignationHandler,
        IDecommissionHandler decommissionHandler,
        IDecommissionPrepHandler decommissionPrepHandler,
        IAdUser adUser)
    {
        _context = context;
        _userOrder = userOrder;
        _reassignationHandler = reassignationHandler;
        _decommissionHandler = decommissionHandler;
        _decommissionPrepHandler = decommissionPrepHandler;
        _adUser = adUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<DeviceDTO>>> GetDevices()
    {
        List<DeviceDTO> devices = await _context.Devices.AsNoTracking().Select(d => new DeviceDTO()
        {
            Id = d.Id,
            ServiceTag = d.ServiceTag,
            DeviceName = d.DeviceName,
            AcquiredDate = d.AcquiredDate.NullableDateToString(),
            DeviceModel = d.DeviceModel,
            PONumber = d.PONumber,
            DeviceStatusId = d.DeviceStatus!.Id,
            DeviceStatusName = d.DeviceStatus!.StatusName,
            DeviceTypeId = d.DeviceType!.Id,
            DeviceTypeName = d.DeviceType!.Name
        //    LastUser1 = _userOrder.GetUserOfDeviceByOrder(d, 1),
        //    LastUser2 = _userOrder.GetUserOfDeviceByOrder(d, 2),
    }).ToListAsync();
        return devices;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeviceDTO>> GetDevice(int id)
    {
        var device = await _context.Devices.AsNoTracking().Include(d => d.DeviceStatus).Include(d => d.DeviceType).FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
        {
            return NotFound();
        }

        string? wdStr = device.AcquiredDate.ToString();
        DeviceDTO deviceDTO = new DeviceDTO()
        {
            Id = device.Id,
            ServiceTag = device.ServiceTag,
            DeviceName = device.DeviceName,
            AcquiredDate = device.AcquiredDate.NullableDateToString(),
            DeviceModel = device.DeviceModel,
            PONumber = device.PONumber,
            DeviceStatusId = device.DeviceStatus!.Id,
            DeviceStatusName = device.DeviceStatus!.StatusName,
            DeviceTypeId = device.DeviceType!.Id,
            DeviceTypeName = device.DeviceType!.Name,

            CurrentUsername = await _userOrder.GetUserOfDeviceByOrderAsync(device.Id, 0),
            LastUser1 = await _userOrder.GetUserOfDeviceByOrderAsync(device.Id, 1),
            LastUser2 = await _userOrder.GetUserOfDeviceByOrderAsync(device.Id, 2)
        };

        return deviceDTO;
    }

    // api/device/query?userName=aaa&deviceTypeId=1
    [HttpGet("Query")]
    public async Task<ActionResult<Device>> GetCurrentDeviceOfUserGivenType(string userName, int deviceTypeId)
    {
        //Get the device that the user is currently using with the given device type.
        UserDevice? userDevice = await _context.UserDevices.FirstOrDefaultAsync(
            ud => ud.UserName == userName && ud.Device.DeviceTypeId == deviceTypeId && ud.UserOrderId == 0
        );

        if (userDevice == null)
        {
            return NoContent();
        }

        Device? device = await _context.Devices.FindAsync(userDevice.DeviceId);

        if (device == null)
        {
            return NoContent();
        }

        return device;
    }

    // api/devices/status?status=1
    [HttpGet("Status")]
    public async Task<ActionResult<List<DeviceDTO>>> GetDevicesBasedOnStatus(int statusId)
    {
        return await _context.Devices.Include(d => d.DeviceType).AsNoTracking().Where(d => d.DeviceStatusId == statusId).Select(d => new DeviceDTO()
        {
            Id = d.Id,
            ServiceTag = d.ServiceTag,
            DeviceName = d.DeviceName,
            AcquiredDate = d.AcquiredDate.NullableDateToString(),
            DeviceTypeId = d.DeviceType!.Id,
            DeviceTypeName = d.DeviceType!.Name,
        }).ToListAsync();
    }

    [HttpGet("Current")]
    public async Task<ActionResult<UserDTO>> GetCurrentUserOfDevice(int deviceId)
    {
        UserDevice? currentDevice = await _context.UserDevices.FirstOrDefaultAsync(ud =>
            ud.DeviceId == deviceId && ud.UserOrderId == 0);

        if (currentDevice is null)
        {
            return NotFound("Cannot find the current user of this device");
        }

        string userName = currentDevice.UserName!;
        string userFullName = _adUser.GetDisplayName(userName);
        string userEmail = _adUser.GetEmail(userName);

        return new UserDTO()
        {
            UserName = userName,
            DisplayName = userFullName,
            Email = userEmail
        };
    }

    // api/device/decommission?deviceId=1
    [HttpGet("Decommissioned")]
    public async Task<ActionResult> DecommissionDevice(int deviceId)
    {
        await _decommissionHandler.HandleDecommissionAsync(deviceId);
        return NoContent();
    }

    /*

    [HttpGet("DecommissionPreparation")]
    public async Task<ActionResult> PrepareDeviceForDecommission(int deviceId)
    {
        await _decommissionPrepHandler.HandleDecommissionPrepAsync(deviceId);
        return NoContent();
    }

    

    [HttpGet("Restock")]
    public async Task<ActionResult> Restock(int deviceId)
    {
        await _decommissionPrepHandler.HandleRestockAsync(deviceId);
        return NoContent();
    }

    */


    [HttpPost]
    public async Task<ActionResult<Device>> AddDevice(DeviceDTO deviceDTO)
    {
        DeviceType? deviceType = await _context.DeviceTypes.FindAsync(deviceDTO.DeviceTypeId);
        if (deviceType == null || deviceType.IsIdentified == false)
        {
            return BadRequest("Device Type is invalid");
        }

        deviceDTO.DeviceStatusId = 1; //Newly added devices will always have an in-stock status.
        Device device = DeviceDTO.ToDevice(deviceDTO);

        _context.Devices.Add(device);

        Transfer transfer = new()
        {
            Device = device,
            TransferFromDestinationId = (int)TransferDestinations.Vendor,
            TransferToDestinationId = (int)TransferDestinations.Stock,
            TransferDate = DateTime.Now,
            TransferTypeId = 1
        };

        _context.Transfers.Add(transfer);

        await _context.SaveChangesAsync();

        return CreatedAtAction("GetDevice", new { id = device.Id }, device);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> EditDevice(int id, DeviceDTO deviceDTO)
    {
        if (id != deviceDTO.Id)
        {
            return BadRequest();
        }

        Device device = DeviceDTO.ToDevice(deviceDTO);

        _context.Entry(device).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }

        return NoContent();
    }



    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device == null)
        {
            return NotFound();
        }

        _context.Devices.Remove(device);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpGet]
    [Route("IsDupeServiceTag")]
    public bool IsDupeServiceTag(string serviceTag, int? deviceId = 0)
    {
        return _context.Devices.Any(d => d.ServiceTag.ToLower() == serviceTag.ToLower() && d.Id != deviceId);
    }

}
