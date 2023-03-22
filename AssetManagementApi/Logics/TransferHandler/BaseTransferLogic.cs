using AssetManagementApi.Models;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Models.AdUserGeneration;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Logics.TransferHandler;

public class BaseTransferLogic
{
    protected readonly AssetContext _assetContext = null!;
    protected readonly IEmailHandler _emailHandler = null!;
    protected readonly IAdUser _adUser = null!;

    protected int _deviceId;
    protected string? _userName;

    protected Guid? _token;

    public BaseTransferLogic()
    {
    }

    public BaseTransferLogic(AssetContext assetContext)
    {
        _assetContext = assetContext;
    }

    public BaseTransferLogic(AssetContext assetContext, IEmailHandler emailHandler)
    {
        _assetContext = assetContext;
        _emailHandler = emailHandler;
    }

    public BaseTransferLogic(AssetContext assetContext, IEmailHandler emailHandler, IAdUser adUser)
    {
        _assetContext = assetContext;
        _emailHandler = emailHandler;
        _adUser = adUser;
    }

    /// <summary>
    /// Update status of the device
    /// </summary>
    /// <param name="statusCode">Status code indicating the status of the device</param>
    /// <returns>The new status of the device</returns>
    public async Task UpdateDeviceStatusAsync(int statusCode)
    {
        Device? device = await _assetContext.Devices.FirstOrDefaultAsync(d => d.Id == _deviceId);
        if (device is not null)
        {
            device.DeviceStatusId = statusCode;
            _assetContext.Entry(device).State = EntityState.Modified;
        }
    }

    /// <summary>
    /// Update new user-device pair to the db and the timeline of the device
    /// </summary>
    /// <param name="orderUserId">The n-th times the device is being used by different users</param>
    /// <returns>The new user device record</returns>
    public async Task AddUserDeviceAsync(int userOrderId)
    {
        UserDevice userDevice = new()
        {
            DeviceId = _deviceId,
            UserName = _userName,
            UserOrderId = userOrderId,
            FirstAssignedDate = DateTime.Now
        };
        await _assetContext.UserDevices.AddAsync(userDevice);
    }

    /// <summary>
    /// Move the ordering use of the device down to 1
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public async Task UpdateUserDeviceAsync()
    {
        await DescendUserOrderAsync();
    }

    /// <summary>
    /// Move the ordering use of the device down to 1
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public async Task DescendUserOrderAsync()
    {

        //First, delete the record whose UserOrderId is 2
        UserDevice? userDevice = await GetUserOfDeviceByOrderId(2);
        if (userDevice != null)
        {
            _assetContext.Remove(userDevice);
        }

        //Then, move the orderId from 1 to 2, from 0 to 1
        userDevice = await GetUserOfDeviceByOrderId(1);
        if (userDevice != null)
        {
            userDevice.UserOrderId = 2;
            _assetContext.Entry(userDevice).State = EntityState.Modified;
        }

        userDevice = await GetUserOfDeviceByOrderId(0);
        if (userDevice != null)
        {
            userDevice.UserOrderId = 1;
            _assetContext.Entry(userDevice).State = EntityState.Modified;
        }
    }

    /// <summary>
    /// Update this transfer to the transfer history table. Depending on the transfer type, the destinations will differ.
    /// </summary>
    /// <param name="transferTypeId">the type of transfer</param>
    /// <returns></returns>
    public virtual async Task UpdateTransferHistoryAsync(int transferTypeId)
    {
        Transfer transfer = new()
        {
            TransferTypeId = transferTypeId,
            DeviceId = _deviceId,
            TransferDate = DateTime.UtcNow.AddHours(7)
        };

        (transfer.TransferFromDestinationId, transfer.TransferToDestinationId) = (transferTypeId) switch
        {
            (2) => (TransferDestinations.Stock, TransferDestinations.User), // Stock to user (assignation)
            (4) => (TransferDestinations.User, TransferDestinations.Stock), // User to stock (withdrawal)
            (5) => (TransferDestinations.Stock, TransferDestinations.Accountant), // Stock - inactive to accountant (decommission)
            (6) => _userName == null ? (TransferDestinations.Stock, TransferDestinations.Manufacturer) 
                : (TransferDestinations.User, TransferDestinations.Manufacturer), // stock to manufacturer, or user to manufacturer (maintenance)
            (7) => (TransferDestinations.Manufacturer, TransferDestinations.Stock), // Manufacturer to stock. (maintenance - done)
            _ => (0, 0)  // Invalid
        };

        UserDevice? currentUserDevice = await GetUserOfDeviceByOrderId(0);

        switch (transferTypeId)
        {
            case (2):
                transfer.TransferToUser = _userName;
                break;
            case (4):
            case (6):
                transfer.TransferFromUser = currentUserDevice != null ? currentUserDevice.UserName : null;
                break;
            default:
                break;
        }
        await _assetContext.Transfers.AddAsync(transfer);
    }

    /// <summary>
    /// Send the email to the owner, only call if all of the required operations are successful.
    /// </summary>
    /// <returns></returns>
    public async Task SendSuccessNotificationEmailAsync(string subject, string emailMsg)
    {
        EmailAdministrator? emailAdmin = await _assetContext.EmailAdministrators.FindAsync(2);
        if (emailAdmin is null) throw new ArgumentException("Can not find the admin email!");
        MailInfo mailInfo = new()
        {
            To = emailAdmin.EmailAdmin,
            Subject = subject,
            HtmlBody = emailMsg
        };
        await _emailHandler.SendEmailAsync(mailInfo);
    }

    /// <summary>
    /// Send the email to the owner with the error information if one of the processes required failed.
    /// </summary>
    /// <param name="errMessage">the message of the error.</param>
    /// <returns></returns>
    public async Task SendErrorNotificationEmailAsync(string errMessage)
    {
        EmailAdministrator? emailAdmin = await _assetContext.EmailAdministrators.FindAsync(2);
        if (emailAdmin is null) throw new ArgumentException("Can not find the admin email!");
        MailInfo mailInfo = new()
        {
            To = emailAdmin.EmailAdmin,
            Subject = "User has confirmed, but an error has occured",
            HtmlBody = @$"<p>The user {_userName} has confirmed the email. But an error has occured while updating the entry.</p>
                <p>Error message: {errMessage}"
        };
        await _emailHandler.SendEmailAsync(mailInfo);
    }

    /// <summary>
    /// Delete the token that has been processed
    /// </summary>
    /// <returns></returns>
    public async Task DeleteProcessedTokenAsync()
    {
        ConfirmationToken? ct = await _assetContext.ConfirmationTokens.FirstOrDefaultAsync(c => c.Token == _token);
        if (ct is not null)
        {
            _assetContext.ConfirmationTokens.Remove(ct);
        }
    }

    public async Task<UserDevice?> GetUserOfDeviceByOrderId(int orderId) =>
    await _assetContext.UserDevices.FirstOrDefaultAsync(ud => ud.DeviceId == _deviceId && ud.UserOrderId == orderId);

    public async Task<Device> GetCurrentDeviceOfUserGivenDeviceType(int deviceTypeId)
    {

        var deviceType = await _assetContext.DeviceTypes.FindAsync(deviceTypeId);
        string deviceTypeName = (deviceType is null) ? string.Empty : deviceType.Name;
        string errMessage = $"Cannot find the {deviceTypeName} that the user {_userName} is currently using";

        UserDevice? userDevice = await _assetContext.UserDevices.FirstOrDefaultAsync(
            ud => ud.UserName == _userName && ud.Device.DeviceTypeId == deviceTypeId && ud.UserOrderId == 0
        );

        if (userDevice is null)
        {
            throw new ArgumentException(errMessage);
        }

        Device? device = await _assetContext.Devices.FindAsync(userDevice.DeviceId);

        if (device is null)
        {
            throw new ArgumentException(errMessage);
        }

        return device;
    }

    public async Task SendEmailAndThrowErrorAsync(string errMessage)
    {
        await SendErrorNotificationEmailAsync(errMessage);
        throw new ArgumentException(errMessage);
    }

    public string GetDisplayNameOfUser()
    {
        User? user = _adUser.Users.FirstOrDefault(u => u.UserName?.ToLower() == _userName?.ToLower());
        if (user is null)
        {
            throw new ArgumentException("Cannot find the given user to process");
        }

        return user.DisplayName!;
    }

    public async Task<bool> IsIdentified(int deviceTypeId)
    {
        DeviceType? deviceType = await _assetContext.DeviceTypes.FindAsync(deviceTypeId);
        if (deviceType == null)
        {
            throw new ArgumentException("Cannot find Device Type");
        }
        return deviceType.IsIdentified;
    }
}

public enum TransferReassignationStep { Assignation, Withdrawal }
