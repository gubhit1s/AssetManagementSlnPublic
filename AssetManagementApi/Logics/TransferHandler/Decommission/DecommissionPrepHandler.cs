using AssetManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Logics.TransferHandler.Decommission;

public class DecommissionPrepHandler : BaseTransferLogic, IDecommissionPrepHandler
{
    public ILogger<DecommissionPrepHandler> _logger;
    public DecommissionPrepHandler(AssetContext assetContext, ILogger<DecommissionPrepHandler> logger) : base(assetContext)
    {
        _logger = logger;
    }

    public async Task HandleDecommissionPrepAsync(int deviceId)
    {

        //Check for validity.
        Device? device = await _assetContext.Devices.FindAsync(deviceId);
        string errMessage = "";
        if (device is null)
        {
            errMessage = $"Cannot find this device {deviceId} to discard";
            throw new ArgumentException(errMessage);
        }
        if (device.DeviceStatusId != 1)
        {
            errMessage = "You can only put a device that's in stock to the decommission preparation list.";

            throw new ArgumentException(errMessage);
        }
        _deviceId = deviceId;

        //Perform the required process.
        await UpdateDeviceStatusAsync(3);
        await UpdateTransferHistoryAsync(8);


        try
        {
            await _assetContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            await SendErrorNotificationEmailAsync(ex.Message);
        }
    }

    public async Task HandleRestockAsync(int deviceId)
    {

        //Check for validity.
        Device? device = await _assetContext.Devices.FindAsync(deviceId);
        string errMessage = "";
        if (device is null)
        {
            errMessage = $"Cannot find this device {deviceId} to restock";
            throw new ArgumentException(errMessage);
        }
        if (device.DeviceStatusId != 3)
        {
            errMessage = "You can only restock a decommissioning device.";

            throw new ArgumentException(errMessage);
        }
        _deviceId = deviceId;

        //Perform the required process.
        await UpdateDeviceStatusAsync(1);
        await UpdateTransferHistoryAsync(9);

        try
        {
            await _assetContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            await SendErrorNotificationEmailAsync(ex.Message);
        }
    }
}