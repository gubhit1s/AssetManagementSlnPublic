using AssetManagementApi.Models;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Logics.TransferHandler.Withdrawal;
using AssetManagementApi.Logics.TransferHandler.Assignation;
using AssetManagementApi.Models.AdUserGeneration;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Logics.TransferHandler.Reassignation;

public class ReassignationHandler : BaseTransferLogic, IReassignationHandler
{

    private readonly IWithdrawalHandler _withdrawalHandler;
    private readonly IAssignationHandler _assignationHandler;
    private readonly ILogger<ReassignationHandler> _logger;

    public ReassignationHandler(
        AssetContext assetContext,
        IEmailHandler emailHandler,
        ILogger<ReassignationHandler> logger,
        IWithdrawalHandler withdrawalHandler,
        IAssignationHandler assignationHandler,
        IAdUser adUser)
        : base(assetContext, emailHandler, adUser)
    {
        _withdrawalHandler = withdrawalHandler;
        _assignationHandler = assignationHandler;
        _logger = logger;
    }

    public async Task HandleReassignationAsync(int deviceId, string userName, bool sendEmail, Guid? token = null)
    {
        _logger.LogInformation("Reassignation background job started.");

        //Check for validity.
        string errMsg = "";
        _userName = userName;

        //Get the device needed to assign
        Device? deviceToAssign = await _assetContext.Devices.FindAsync(deviceId);
        if (deviceToAssign is null || deviceToAssign.DeviceStatusId != 1)
        {
            errMsg = $"Cannot find the device {deviceId} to reassign or the device is found but is not currently in stock.";
            await SendErrorNotificationEmailAsync(errMsg);
            throw new ArgumentException(errMsg);
        }


        try
        {
            //Get the current device of the user.
            int dtId = deviceToAssign.DeviceTypeId;
            Device deviceAssigned = await GetCurrentDeviceOfUserGivenDeviceType(dtId);

            //Move the current device to stock by withdrawing it
            await _withdrawalHandler.HandleWithdrawalAsync(deviceAssigned.Id, userName, token, false);

            //Assign deviceToAssign to the specified user
            await _assignationHandler.HandleAssignationAsync(deviceToAssign.Id, userName, token, false);

            if (sendEmail)
            {
                //Then we're done!
                string? userDisplayName = GetDisplayNameOfUser();
                {
                    await SendSuccessNotificationEmailAsync(
                        "Assignation: User has confirmed",
                        $"<p>User <b>{userDisplayName}</b> has confirmed the reassignation of device with service tag: <b>{deviceToAssign.ServiceTag}</b>. The inventory has successfully been updated.</p>");
                }
            }
        }
        catch (Exception e)
        {
            await SendErrorNotificationEmailAsync(e.Message);
            throw;
        }
        finally
        {
            _logger.LogInformation("Reassignation background job finished.");
        }
    }
}




