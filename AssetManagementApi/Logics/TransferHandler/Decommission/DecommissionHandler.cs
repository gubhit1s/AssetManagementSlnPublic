using AssetManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Logics.EmailHandler;

namespace AssetManagementApi.Logics.TransferHandler.Decommission;

public class DecommissionHandler : BaseTransferLogic, IDecommissionHandler {

    private readonly ILogger<DecommissionHandler> _logger;
    public DecommissionHandler(AssetContext assetContext, IEmailHandler emailHandler, ILogger<DecommissionHandler> logger) : base(assetContext, emailHandler)
    {
        _logger = logger;
    }

    public async Task HandleDecommissionAsync(int deviceId)
    {
        _logger.LogInformation("Background job decommission started");

        //Check for validity
        Device? device = await _assetContext.Devices.FindAsync(deviceId);
        string errMessage = "";
        if (device is null)
        {
            errMessage = $"Cannot find this device {deviceId} to decommission!";
            throw new ArgumentException(errMessage);
        }
        if (device.DeviceStatusId != (int)DeviceStatuses.InStock && device.DeviceStatusId != (int)DeviceStatuses.Damaged)
        {
            errMessage = "You can only decommission a device that's in stock or damaged!";
            
            throw new ArgumentException(errMessage);
        }
        _deviceId = deviceId;

        await UpdateDeviceStatusAsync((int)DeviceStatuses.Decommissioned);
        await UpdateTransferHistoryAsync(5);

        try
        {
            await _assetContext.SaveChangesAsync();
            {
                _logger.LogInformation("Background job decommission finished.");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}
