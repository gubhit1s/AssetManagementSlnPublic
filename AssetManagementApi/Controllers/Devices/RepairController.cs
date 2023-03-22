using AssetManagementApi.Models;
using AssetManagementApi.Logics.TransferHandler.Repair;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagementApi.Controllers;

[Authorize(Roles = "Administrator")]
[ApiController]
[Route("api/devices")]
public class RepairController : ControllerBase
{

    private readonly RepairHandler _repairHandler;
    private readonly AssetContext _assetContext;

    public RepairController(RepairHandler repairHandler, AssetContext assetContext)
    {
        _repairHandler = repairHandler;
        _assetContext = assetContext;
    }

    [HttpGet]
    [Route("repairing")]
    public async Task<ActionResult> MoveDeviceToMaintainingList(int deviceId)
    {
        try
        {
            await _repairHandler.HandleRepairFromStock(deviceId);
            return NoContent();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("repaired")]
    public async Task<ActionResult> MoveBrokenDeviceBackToStock(int deviceId, bool repairSuccess)
    {
        try
        {
            await _repairHandler.HandleRepairFromManufacturer(deviceId, repairSuccess);
            return NoContent();
        }
        catch (Exception)
        {
            throw;
        }
    }
}