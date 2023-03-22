using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Models;
using AssetManagementApi.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagementApi.Controllers;

[Authorize(Roles = "Administrator")]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{

    private readonly AssetContext _assetContext;

    public DashboardController(AssetContext assetContext)
    {
        _assetContext = assetContext;
    }

    [HttpGet("deviceoverviews")]
    public async Task<ActionResult<List<DeviceOverviewDTO>>> GetDeviceOverviewsAsync()
    {
        List<DeviceOverviewDTO> identified = await GetDeviceIdentifiedOverviewAsync();
        List<DeviceOverviewDTO> unidentified = await GetDeviceUnidentifiedOverviewAsync();
        return identified.Concat(unidentified).ToList();
    }

    private async Task<List<DeviceOverviewDTO>> GetDeviceIdentifiedOverviewAsync()
    {
        //Count the number of each identified device types.
        List<DeviceOverviewDTO> result = new();
        var uniqueDeviceTypes = await _assetContext.DeviceTypes.AsNoTracking()
            .Where(dt => dt.IsIdentified == true).Select(dt => dt.Id).ToListAsync();
        //Create a record for each of the device type.
        foreach (int dt in uniqueDeviceTypes)
        {
            string deviceTypeName = await _assetContext.GetDeviceTypeNameAsync(dt);

            int total = await _assetContext.Devices.AsNoTracking().CountAsync(d => d.DeviceTypeId == dt && d.DeviceStatusId != (int)DeviceStatuses.Decommissioned);
            int inStock = await _assetContext.Devices.AsNoTracking().CountAsync(d => d.DeviceTypeId == dt &&
                d.DeviceStatusId == (int)DeviceStatuses.InStock);
            int inUse = await _assetContext.Devices.AsNoTracking().CountAsync(d => d.DeviceTypeId == dt &&
                d.DeviceStatusId == (int)DeviceStatuses.InUse);

            DeviceOverviewDTO overview = new()
            {
                DeviceTypeId = dt,
                DeviceTypeName = deviceTypeName,
                TotalAmount = total,
                InStockAmount = inStock,
                InUseAmount = inUse
            };

            result.Add(overview);
        }
        return result;
    }

    private async Task<List<DeviceOverviewDTO>> GetDeviceUnidentifiedOverviewAsync()
    {
        //Get unique unidentified types.
        List<DeviceOverviewDTO> result = new();
        var deviceTypes = await _assetContext.DeviceTypes.Where(dt => dt.IsIdentified == false).Select(dt => dt.Id).ToListAsync();
        foreach (int dtId in deviceTypes)
        {
            int total = _assetContext.DevicesUnidentified.AsNoTracking().Where(du => du.DeviceTypeId == dtId && du.DeviceStatusId != (int)DeviceStatuses.Decommissioned).Sum(d => d.Amount);
            int inStock = (await _assetContext.DevicesUnidentified.AsNoTracking().FirstOrDefaultAsync(
                du => du.DeviceTypeId == dtId && du.DeviceStatusId == (int)DeviceStatuses.InStock))!.Amount;
            int inUse = (await _assetContext.DevicesUnidentified.AsNoTracking().FirstOrDefaultAsync(
                du => du.DeviceTypeId == dtId && du.DeviceStatusId == (int)DeviceStatuses.InUse))!.Amount;

            DeviceOverviewDTO overview = new()
            {
                DeviceTypeId = dtId,
                DeviceTypeName = await _assetContext.GetDeviceTypeNameAsync(dtId),
                TotalAmount = total,
                InStockAmount = inStock,
                InUseAmount = inUse
            };
            result.Add(overview);
        }
        return result;
    }
}
