using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferHistoryController : ControllerBase
{

    private readonly AssetContext _assetContext;

    public TransferHistoryController(AssetContext assetContext)
    {
        _assetContext = assetContext;
    }

    [HttpGet("completed/identified")]
    public async Task<ActionResult<List<CompletedTransferDTO>>> GetCompletedTransferDetails(DateTime? FromDate, DateTime? ToDate)
    {
        IQueryable<CompletedTransferDTO> transferHistories = _assetContext.Transfers.AsNoTracking()
            .Include(t => t.TransferFromDestination)
            .Include(t => t.TransferToDestination)
            .Include(t => t.TransferTypeRef)
            .Include(t => t.Device)
            .ThenInclude(d => d.DeviceType)
            .Select(t => new CompletedTransferDTO()
            {
                TransferDate = t.TransferDate,
                DeviceServiceTag = t.Device.ServiceTag,
                DeviceTypeName = t.Device.DeviceType!.Name,
                TransferFrom = t.TransferFromDestinationId == 3 ? t.TransferFromUser : t.TransferFromDestination.Destination,
                TransferTo = t.TransferToDestinationId == 3 ? t.TransferToUser : t.TransferToDestination.Destination,
                TransferTypeName = t.TransferTypeRef.Name
            });
            
        DateTime toDate = DateTime.Now;
        if (ToDate != null)
        {
            toDate = (DateTime)ToDate;
        }

        if (FromDate == null || ToDate == null)
        {
            return await transferHistories.ToListAsync();
        }
        //we assume to retrieve inclusively the transfers of to date, so we plus 1 to that.
        else
        {
            return await transferHistories.Where(t => t.TransferDate >= FromDate && t.TransferDate <= toDate.AddDays(1)).ToListAsync();
        }

    }

    [HttpGet("completed/unidentified")]
    public async Task<ActionResult<List<CompletedTransferDTO>>> GetCompletedTransferDetailsUd(DateTime? FromDate, DateTime? ToDate)
    {
        IQueryable<CompletedTransferDTO> transferHistories = _assetContext.TransfersUd.AsNoTracking()
            .Include(t => t.TransferFromDestination)
            .Include(t => t.TransferToDestination)
            .Include(t => t.TransferTypeRef)
            .Include(t => t.DeviceType)
            .Select(t => new CompletedTransferDTO()
            {
                TransferDate = t.TransferDate,
                DeviceTypeName = t.DeviceType!.Name,
                TransferFrom = t.TransferFromDestinationId == 3 ? t.TransferFromUser : t.TransferFromDestination.Destination,
                TransferTo = t.TransferToDestinationId == 3 ? t.TransferToUser : t.TransferToDestination.Destination,
                Amount = t.Amount,
                TransferTypeName = t.TransferTypeRef.Name
            });
            
        DateTime toDate = DateTime.Now;
        if (ToDate != null)
        {
            toDate = (DateTime)ToDate;
        }

        if (FromDate == null || ToDate == null)
        {
            return await transferHistories.ToListAsync();
        }
        //we assume to retrieve inclusively the transfers of to date, so we plus 1 to that.
        else
        {
            return await transferHistories.Where(t => t.TransferDate >= FromDate && t.TransferDate <= toDate.AddDays(1)).ToListAsync();
        }

    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingTransferDTO>>> GetPendingTransferDetails()
    {
        var pendingIdentified = await _assetContext.ConfirmationTokens.AsNoTracking()
            .Include(ct => ct.Device).ThenInclude(ct => ct!.DeviceType)
            .Include(ct => ct.TransferType)
            .Select(ct => new PendingTransferDTO()
            {
                ServiceTag = ct.Device!.ServiceTag,
                DeviceTypeName = ct.Device.DeviceType!.Name,
                TransferTypeName = ct.TransferType!.Name,
                UserName = ct.UserName!,
                ExpiryDate = ct.ExpiryDate
            }).ToListAsync();

        var pendingUnidentified = await _assetContext.ConfirmationTokensUd.AsNoTracking()
            .Include(ctu => ctu.DeviceType)
            .Include(ctu => ctu.TransferType)
            .Select(ctu => new PendingTransferDTO()
            {
                DeviceTypeName = ctu.DeviceType!.Name,
                TransferTypeName = ctu.TransferType!.Name,
                UserName = ctu.UserName!,
                ExpiryDate = ctu.ExpiryDate
            }).ToListAsync();

        return pendingIdentified.Concat(pendingUnidentified).ToList();

    }


}

