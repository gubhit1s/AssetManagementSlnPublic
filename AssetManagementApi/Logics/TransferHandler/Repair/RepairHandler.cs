using AssetManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Logics.TransferHandler;
using AssetManagementApi.Logics.TransferHandler.Assignation;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Models.AdUserGeneration;

namespace AssetManagementApi.Logics.TransferHandler.Repair;

public class RepairHandler : BaseTransferLogic
{

    private ILogger<RepairHandler> _logger;
    private IAssignationHandler _assignationHandler;
    public RepairHandler(AssetContext assetContext, IEmailHandler emailHandler, ILogger<RepairHandler> logger,
        IAssignationHandler assignationHandler, IAdUser adUser) : base(assetContext, emailHandler, adUser)
    {
        _logger = logger;
        _assignationHandler = assignationHandler;

    }

    /// <summary>
    /// Move the device from stock or user to the manufacturer
    /// </summary>
    /// <param name="deviceId">the id of the device</param>
    /// <param name="userName">the username, if transferring from the user.</param>
    /// <returns></returns>
    public async Task MoveDeviceFromOfficeToManufacturerAsync(int deviceId)
    {
        _deviceId = deviceId;
        await UpdateDeviceStatusAsync((int)DeviceStatuses.Maintaining);
        if (_userName != null)
        {
            await UpdateUserDeviceAsync();
        }
        
        await UpdateTransferHistoryAsync(6);
    }

    public async Task HandleRepairFromUser(int newDeviceId, string userName, bool sendEmail)
    {
        _userName = userName;
        _logger.LogInformation("Background job reparation started.");
        Device? newDeviceToAssign = await _assetContext.Devices.FindAsync(newDeviceId);
        if (newDeviceToAssign is null)
        {
            await SendEmailAndThrowErrorAsync($"Cannot find new device to assign to user {userName}");
        }
        if (newDeviceToAssign!.DeviceStatusId != 1)
        {
            await SendEmailAndThrowErrorAsync($"This device is not currently in stock to assign.");
        }

        //Get the current device of the user in context.
        int deviceTypeInProcess = newDeviceToAssign!.DeviceTypeId;
        Device deviceToMove = await GetCurrentDeviceOfUserGivenDeviceType(deviceTypeInProcess);
        await MoveDeviceFromOfficeToManufacturerAsync(deviceToMove.Id);
        await _assignationHandler.HandleAssignationAsync(newDeviceToAssign.Id, userName, null, false);

        try
        {
            await _assetContext.SaveChangesAsync();
            string displayName = GetDisplayNameOfUser();
            if (sendEmail)
            {
                await SendSuccessNotificationEmailAsync("Repair and Reassign: User has confirmed",
                    $"<p>User <b>{displayName}</b> has confirmed the reassignation of device <b>{newDeviceToAssign.ServiceTag}</b>. The inventory has successfully been updated.</p>");
            };
        }

        catch (DbUpdateConcurrencyException e)
        {
            await SendErrorNotificationEmailAsync(e.Message);
        }
        finally
        {
            _logger.LogInformation("Background job reparation finished.");
        }

    }

    public async Task HandleRepairFromStock(int deviceId)
    {
        Device? device = await _assetContext.Devices.FindAsync(deviceId);
        if (device is null)
        {
            throw new ArgumentException($"Cannot find device to process.");
        }
        if (device!.DeviceStatusId != (int)DeviceStatuses.InStock)
        {
            throw new ArgumentException($"This device is not currently to stock to repair.");
        }

        await MoveDeviceFromOfficeToManufacturerAsync(deviceId);
        try
        {
            await _assetContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
    }

    public async Task HandleRepairFromManufacturer(int deviceId, bool repairSuccess)
    {
        Device? device = await _assetContext.Devices.FindAsync(deviceId);
        if (device is null)
        {
            throw new ArgumentException($"Cannot find device to process.");
        }
        if (device!.DeviceStatusId != (int)DeviceStatuses.Maintaining)
        {
            throw new ArgumentException($"You can only recover a device from the manufacturer!");
        }

        _deviceId = deviceId;

        if (repairSuccess)
        {
            await UpdateDeviceStatusAsync((int)DeviceStatuses.InStock);
        } else
        {
            await UpdateDeviceStatusAsync((int)DeviceStatuses.Damaged);
        }

        await UpdateTransferHistoryAsync(TransferTypes.MaintenanceRepaired);

        try
        {
            await _assetContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
    }


}
