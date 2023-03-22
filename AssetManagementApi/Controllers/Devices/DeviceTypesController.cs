using AssetManagementApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagementApi.Controllers;

[Authorize(Roles = "Administrator")]
[Route("api/[controller]")]
[ApiController]
public class DeviceTypesController : ControllerBase
{

    private readonly AssetContext _context;

    public DeviceTypesController(AssetContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<DeviceType>>> GetDeviceTypes(bool? identified)
    {
        if (identified == null)
        {
            return await _context.DeviceTypes.AsNoTracking().ToListAsync();
        }
        else
        {
           return await _context.DeviceTypes.Where(dt => dt.IsIdentified == identified).AsNoTracking().ToListAsync();
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeviceType>> GetDeviceType(int id)
    {

        var deviceType = await _context.DeviceTypes.FindAsync(id);

        if (deviceType == null)
        {
            return NotFound();
        }

        return deviceType;
    }

    [HttpPost]
    public async Task<ActionResult<DeviceType>> AddDeviceType(DeviceType deviceType)
    {
        await _context.DeviceTypes.AddAsync(deviceType);

        //If this device type is unidentified, create new overview rows with default status values.
        if (!deviceType.IsIdentified)
        {
            var deviceStatuses = await _context.DeviceStatusTypes.ToListAsync();
            foreach (DeviceStatusType status in deviceStatuses)
            {
                DeviceUnidentified dtOv = new DeviceUnidentified()
                {
                    DeviceType = deviceType,
                    DeviceStatusId = status.Id,
                    Amount = 0,
                };
                await _context.DevicesUnidentified.AddAsync(dtOv);
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction("GetDeviceType", new { id = deviceType.Id }, deviceType);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditDeviceType(int id, DeviceType deviceType)
    {
        if (id != deviceType.Id)
        {
            return BadRequest();
        }

        _context.Entry(deviceType).State = EntityState.Modified;

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
    public async Task<IActionResult> DeleteDeviceType(int id)
    {
        var deviceType = await _context.DeviceTypes.FindAsync(id);

        if (deviceType == null)
        {
            return NotFound();
        }

        //Check if this device type has any devices, if yes we threw a 405 error
        int devicesOfThisTypeCount = await _context.Devices.CountAsync(d => d.DeviceTypeId == id);
        if (devicesOfThisTypeCount > 0)
        {
            return BadRequest("There are devices of this device type, please remove them first!");
        }

        _context.DeviceTypes.Remove(deviceType);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
