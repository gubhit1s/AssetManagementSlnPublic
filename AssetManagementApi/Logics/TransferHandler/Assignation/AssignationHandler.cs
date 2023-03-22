using AssetManagementApi.Models;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Logics.EmailHandler;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Logics.TransferHandler.Assignation;

public class AssignationHandler : BaseTransferLogic, IAssignationHandler
{

    private readonly ILogger<AssignationHandler> _logger;
    public AssignationHandler(AssetContext assetContext, IEmailHandler emailHandler, ILogger<AssignationHandler> logger,
        IAdUser adUser) : base(assetContext, emailHandler, adUser)
    {
        _logger = logger;
    }

    /// <summary>
    /// Update the new assignation entry to the db and send an email to the assigner if successful.
    /// </summary>
    /// <param name="deviceId">The id of the device.</param>
    /// <param name="userName">The user name retrieved from AD.</param>
    /// <returns></returns>
    public async Task HandleAssignationAsync(int deviceId, string userName, Guid? token, bool sendEmail)
    {
        
        //Check the status of device.
        Device? device = await _assetContext.Devices.FindAsync(deviceId);
        string errMessage = "";
        if (device is null)
        {
            errMessage = $"Cannot find this device {deviceId} to assign";
            await SendErrorNotificationEmailAsync(errMessage);
            throw new ArgumentException(errMessage);
        }

        int deviceStatus = device.DeviceStatusId;
        if (deviceStatus != 1)
        {
            errMessage = $"Can not assign a device that is already in use or decommissioned.";
            await SendErrorNotificationEmailAsync(errMessage);
            throw new ArgumentException(errMessage);
        }

        //Perform the required operation.
        _logger.LogInformation("Background job assignation started");
        _deviceId = deviceId;
        _userName = userName;
        _token = token;
        await UpdateDeviceStatusAsync(2);
        await AddUserDeviceAsync(0);

        //If we don't send the email then we can infer this is an reassignation.
        await UpdateTransferHistoryAsync(2);
        
        try
        {
            await _assetContext.SaveChangesAsync();
            if (sendEmail) 
            {
                string? userDisplayName = GetDisplayNameOfUser();
                {
                    await SendSuccessNotificationEmailAsync(
                        "Assignation: User has confirmed",
                        $"<p>User <b>{userDisplayName}</b> has confirmed the assignation of the device with service tag: <b>{device.ServiceTag}</b>. The inventory has successfully been updated.</p>");
                }
            }

            _logger.LogInformation("Background job assignation finished.");
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


