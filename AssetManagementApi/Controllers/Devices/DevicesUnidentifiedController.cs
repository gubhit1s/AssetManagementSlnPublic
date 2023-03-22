using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Logics.TransferHandler.Unidentified;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagementApi.Controllers;

[Authorize(Roles = "Administrator")]
[ApiController]
[Route("api/devices/unidentified")]
public class DevicesUnidentifiedController : ControllerBase
{

    private readonly AssetContext _assetcontext;
    private readonly UnidentifiedTransferHandler _transferHandler;

    public DevicesUnidentifiedController(AssetContext assetContext, UnidentifiedTransferHandler transferHandler)
    {
        _assetcontext = assetContext;
        _transferHandler = transferHandler;
    }

    [HttpGet]
    public async Task<ActionResult<List<DeviceUnidentifiedDTO>>> GetDevicesUnidentified(int? statusId = null)
    {
        if (statusId == null)
        {
            return await _assetcontext.DevicesUnidentified.AsNoTracking()
                .Include(d => d.DeviceType).Include(d => d.DeviceStatusType).Select(d => new DeviceUnidentifiedDTO()
                {
                    DeviceStatusId = d.DeviceStatusId,
                    DeviceStatusName = d.DeviceStatusType!.StatusName,
                    DeviceTypeId = d.DeviceTypeId,
                    DeviceTypeName = d.DeviceType!.Name,
                    Amount = d.Amount
                }).ToListAsync();
        }
        else
        {
            return await _assetcontext.DevicesUnidentified.AsNoTracking()
                .Include(d => d.DeviceType).Where(d => d.DeviceStatusId == 1).Select(d => new DeviceUnidentifiedDTO()
                {
                    DeviceTypeId = d.DeviceTypeId,
                    DeviceTypeName = d.DeviceType!.Name,
                    DeviceStatusId = d.DeviceStatusId,
                    Amount = d.Amount
                }).ToListAsync();
        }

    }

    [HttpPost]
    public async Task<ActionResult> ManipulateAmountBasedOnTransferType(DeviceUnidentifiedDTO data)
    {
        //Verify the device type.
        DeviceType? deviceType = await _assetcontext.DeviceTypes.FindAsync(data.DeviceTypeId);
        if (deviceType == null || deviceType.IsIdentified)
        {
            return BadRequest("Device type not found or is not identified");
        }
        await _transferHandler.HandleTransferUdDevicesNotInvolvingUserAsync(data);
        return NoContent();

    }

    [HttpGet("Query")]
    public async Task<ActionResult<bool>> GetDeviceTypeUserIsUsing(int deviceTypeId, string userName)
    {
        UserDeviceUd? userDeviceUd = await _assetcontext.UserDevicesUd.FirstOrDefaultAsync(
            udu => udu.DeviceTypeId == deviceTypeId && udu.UserName == userName);

        return userDeviceUd != null;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> EditDevices(int id, DeviceUnidentified data)
    {
        if (id != data.DeviceTypeId)
        {
            return BadRequest();
        }
        
        DeviceUnidentified? du = await _assetcontext.DevicesUnidentified.FirstOrDefaultAsync(
            du => du.DeviceTypeId == id && du.DeviceStatusId == data.DeviceStatusId);

        if (du is null)
        {
            return BadRequest($"Cannot find the device instance of device type {id}");
        }

        _assetcontext.DevicesUnidentified.Entry(du).State = EntityState.Modified;

        return NoContent();

    }


}