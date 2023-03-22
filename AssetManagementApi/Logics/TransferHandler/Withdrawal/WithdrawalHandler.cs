using AssetManagementApi.Models;
using AssetManagementApi.Models.AdUserGeneration;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Logics.EmailHandler;

namespace AssetManagementApi.Logics.TransferHandler.Withdrawal;

public class WithdrawalHandler : BaseTransferLogic, IWithdrawalHandler
{

    private readonly ILogger<WithdrawalHandler> _logger;
    public WithdrawalHandler(AssetContext assetContext, IEmailHandler emailHandler, ILogger<WithdrawalHandler> logger,
        IAdUser adUser) : base(assetContext, emailHandler, adUser)
    {
        _logger = logger;
    }

    public async Task HandleWithdrawalAsync(int deviceId, string userName, Guid? token, bool sendEmail)
    {
        _logger.LogInformation("Background job withdrawal started");
        //Check the status of device.
        Device? device = await _assetContext.Devices.FindAsync(deviceId);
        if (device is null)
        {
            await SendEmailAndThrowErrorAsync($"Cannot find this device {deviceId}");
        }

        int deviceStatus = device!.DeviceStatusId;
        if (deviceStatus != 2)
        {
            await SendEmailAndThrowErrorAsync($"Can not withdraw a device that is already in stock or decomissioned.");
        }

        //Perform the required steps.
        _deviceId = deviceId;
        _token = token;
        _userName = userName;

        await UpdateDeviceStatusAsync(1);
        await UpdateUserDeviceAsync();

        await UpdateTransferHistoryAsync(4);
        
        try
        {
            await _assetContext.SaveChangesAsync();

            if (sendEmail) 
            {
                string userDisplayName = _adUser.GetDisplayName(_userName);
                string userEmail = _adUser.GetEmail(_userName);
                {
                    await SendSuccessNotificationEmailAsync(
                        "Withdrawal: User has confirmed",
                        $"<p>User <b>{userDisplayName}</b>({userEmail}) has confirmed the withdrawal of the device with service tag: <b>{device.ServiceTag}</b>. The inventory has successfully been updated.</p>");
                }
            }

            _logger.LogInformation("Background job withdrawal finished.");
        }

        catch (Exception ex)
        {
            if (sendEmail)
            {
                await SendErrorNotificationEmailAsync(ex.Message);
            }
            throw;
        }
    }
}
