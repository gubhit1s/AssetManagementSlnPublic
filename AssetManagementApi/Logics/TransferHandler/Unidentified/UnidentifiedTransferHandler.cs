using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Logics.EmailHandler;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Logics.TransferHandler.Unidentified;

public class UnidentifiedTransferHandler : BaseTransferLogic
{

    private readonly ILogger<UnidentifiedTransferHandler> _logger;

    private int _deviceTypeId;

    public UnidentifiedTransferHandler(ILogger<UnidentifiedTransferHandler> logger, 
        AssetContext assetContext,
        IEmailHandler emailHandler,
        IAdUser adUser) : base(assetContext, emailHandler, adUser)
    {
        _logger = logger;

    }

    public async Task HandleTransferUdDevicesNotInvolvingUserAsync(DeviceUnidentifiedDTO deviceUnidentified)
    {
        //First, check the existence of the unidentified device type.
        DeviceType? deviceType = await GetDeviceTypeAsync(deviceUnidentified.DeviceTypeId);
        _deviceTypeId = deviceType.Id;

        int dsId = deviceUnidentified.DeviceStatusId;
        int amountToAdd = deviceUnidentified.Amount;

        //Retrieve the instances needed to process.
        DeviceUnidentified inStockInstance = await GetDeviceUnidentifiedAsync(DeviceStatuses.InStock);
        DeviceUnidentified inUseInstance = await GetDeviceUnidentifiedAsync(DeviceStatuses.InUse);
        DeviceUnidentified damagedInstance = await GetDeviceUnidentifiedAsync(DeviceStatuses.Damaged);
        DeviceUnidentified decommissionedInstance = await GetDeviceUnidentifiedAsync(DeviceStatuses.Decommissioned);

        List<DeviceUnidentified> stateInstances = new()
        {
            inStockInstance, inUseInstance, damagedInstance, decommissionedInstance
        };

        int transfer = deviceUnidentified.TransferTypeId;
        //Adjust the amount based on each transfer types.
        switch (transfer)
        {
            case TransferTypes.Boarding: //Boarding
                inStockInstance.Amount += amountToAdd;
                break;
            case TransferTypes.Accident: //Accident - Damaged
                inStockInstance.Amount -= amountToAdd;
                damagedInstance.Amount += amountToAdd;
                break;
            case TransferTypes.Decommission: //Decommission
                decommissionedInstance.Amount += amountToAdd;
                if (dsId == (int)DeviceStatuses.Damaged) damagedInstance.Amount -= amountToAdd;
                else throw new ArgumentException("Device is not in a valid state to decommission");
                break;
            default:
                throw new ArgumentException("Transfer Type is not valid to process");
        }
        await AddTransferHistoryUdAsync(deviceUnidentified.TransferTypeId, amountToAdd);

        //Update and save
        _assetContext.DevicesUnidentified.UpdateRange(stateInstances);
        try
        {
            await _assetContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
    }

    public async Task HandleTransferUdDevicesInvolvingUserAsync(ConfirmationTokenUd data, bool sendEmail)
    {
        //Verify device and user.
        _logger.LogInformation("Background job unidentified device started.");
        User? user = _adUser.Users.FirstOrDefault(u => u.UserName == data.UserName);
        if (user is null)
        {
            await SendEmailAndThrowErrorAsync("Cannot find the user to process.");
        }

        DeviceType? deviceType = await _assetContext.DeviceTypes.FindAsync(data.DeviceTypeId);

        if (deviceType is null)
        {
            await SendEmailAndThrowErrorAsync("Cannot find the proper device type to process.");
        }
        _userName = user!.UserName;
        _deviceTypeId = deviceType!.Id;

        //Retrieve the amount instances needed to change.        
        DeviceUnidentified inStockInstance = await GetDeviceUnidentifiedAsync(DeviceStatuses.InStock);
        DeviceUnidentified inUseInstance = await GetDeviceUnidentifiedAsync(DeviceStatuses.InUse);
        DeviceUnidentified damagedInstance = await GetDeviceUnidentifiedAsync(DeviceStatuses.Damaged);
        List<DeviceUnidentified> stateInstances = new()
        {
            inStockInstance, inUseInstance, damagedInstance,
        };

        string subject = "User has confirmed";;
        string userDisplayName = _adUser.GetDisplayName(_userName!);
        string userEmail = _adUser.GetEmail(_userName!);
        string updated = "The inventory has successfully been updated.";
        string body = "";

        int transfer = data.TransferTypeId;
        switch (transfer)
        {
            case TransferTypes.Assignation:
                if (inStockInstance.Amount <= 0) throw new ArgumentException("Out of devices of this type!");
                await AddUserDeviceUdAsync();
                await AddTransferHistoryUdAsync(2);
                inStockInstance.Amount -= 1;
                inUseInstance.Amount += 1;
                subject = $"Assignation: {subject}";
                body = $"User {userDisplayName}({userEmail}) has confirmed the assignation of a {deviceType.Name}. {updated}";
                break;

            case TransferTypes.Reassignation:
                if (inStockInstance.Amount <= 0) throw new ArgumentException("Out of devices of this type!");
                //Reassignation means both withdrawing and assigning at the same time.
                await AddTransferHistoryUdAsync(2);
                await AddTransferHistoryUdAsync(4);
                subject = $"Reassignation: {subject}";
                body = $"User {userDisplayName}({userEmail}) has confirmed the reassignation of a {deviceType.Name}. {updated}";
                break;

            case TransferTypes.Withdrawal:
                await DeleteUserDeviceUdAsync();
                await AddTransferHistoryUdAsync(4);
                inStockInstance.Amount += 1;
                inUseInstance.Amount -= 1;
                subject = $"Withdrawal: {subject}";
                body = $"User {userDisplayName}({userEmail}) has confirmed the withdrawal of a {deviceType.Name}. {updated}";
                break;

            case TransferTypes.ReassignationDamaged:
                if (inStockInstance.Amount <= 0) throw new ArgumentException("Out of devices of this type!");
                await AddTransferHistoryUdAsync(10);
                damagedInstance.Amount += 1;
                inStockInstance.Amount -= 1;
                subject = $"Reassignation - Damaged: {subject}";
                body = $"User {userDisplayName}({userEmail}) has confirmed the reassignation of a {deviceType.Name} in replacement of a damaged one. {updated}";
                break;

            default:
                throw new ArgumentException("Invalid transfer type to process");
        }
        _assetContext.DevicesUnidentified.UpdateRange(stateInstances);

        try
        {
            await _assetContext.SaveChangesAsync();
            if (sendEmail)
            {
                await SendSuccessNotificationEmailAsync(subject, body);
                _logger.LogInformation("Background job unidentified device finished.");
            }
        }
        catch (DbUpdateConcurrencyException err)
        {
            await SendErrorNotificationEmailAsync(err.Message);
            throw;
        }

    }

    public async Task AddUserDeviceUdAsync()
    {
        //Check if user is using this device type yet.
        bool IsUserUsingThisDeviceType = _assetContext.UserDevicesUd.Any(
            udu => udu.DeviceTypeId == _deviceTypeId && udu.UserName!.ToLower() == _userName!.ToLower());

        if (IsUserUsingThisDeviceType)
        {
            throw new ArgumentException("User is already using this device type!");
        }

        UserDeviceUd udd = new UserDeviceUd()
        {
            DeviceTypeId = _deviceTypeId,
            UserName = _userName,
            FirstAssignedDate = DateTime.Now
        };

        await _assetContext.UserDevicesUd.AddAsync(udd);
    }

    public async Task DeleteUserDeviceUdAsync()
    {
        var userDeviceUd = await GetUserDeviceUdAsync();

        _assetContext.UserDevicesUd.Remove(userDeviceUd);
    }

    public async Task AddTransferHistoryUdAsync(int transferTypeId, int amount = 1)
    {
        TransferUd transferUd = new()
        {
            TransferTypeId = transferTypeId,
            DeviceTypeId = _deviceTypeId,
            TransferDate = DateTime.Now
        };

        (transferUd.TransferFromDestinationId, transferUd.TransferToDestinationId) = (transferTypeId) switch
        {
            (1) => (1, 2), // Vendor to stock.
            (2) => (2, 3), // Stock to user (assignation)
            (4) => (3, 2), // User to stock (withdrawal)
            (5) => (2, 4), // Stock to accountant (decommission)
            (10) => (3, 2), // From User to Stock.
            (11) => (2, 2), // From stock to stock
            _ => throw new ArgumentException("Invalid transfer type to process")  // Invalid
        };

        switch (transferTypeId)
        {
            case (2):
                transferUd.TransferToUser = _userName;
                transferUd.Amount = 1;
                break;
            case (4):
            case (10):
                transferUd.TransferFromUser = _userName;
                transferUd.Amount = 1;
                break;
            default:
                transferUd.Amount = amount;
                break;
        }
        await _assetContext.AddAsync(transferUd);
    }

    public async Task<DeviceUnidentified> GetDeviceUnidentifiedAsync(DeviceStatuses status)
    {
        DeviceUnidentified? result = await _assetContext.DevicesUnidentified.FirstOrDefaultAsync(d =>
            d.DeviceTypeId == _deviceTypeId && d.DeviceStatusId == (int)status);

        if (result == null)
        {
            await SendEmailAndThrowErrorAsync("Cannot find the DeviceUnidentified instance to process.");
        }

        return result!;
    }

    public async Task<DeviceType> GetDeviceTypeAsync(int id)
    {
        var result = await _assetContext.DeviceTypes.FindAsync(id);
        if (result == null || result.IsIdentified == true)
        {
            await SendEmailAndThrowErrorAsync("Device type not found or is not unidentified.");
        }
        return result!;
    }

    public async Task<UserDeviceUd> GetUserDeviceUdAsync()
    {
        var userDeviceUd = await _assetContext.UserDevicesUd.FirstOrDefaultAsync(
            udu => udu.DeviceTypeId == _deviceTypeId && udu.UserName!.ToLower() == _userName!.ToLower());
        if (userDeviceUd == null)
        {
            await SendEmailAndThrowErrorAsync($"Cannot find the device of the user {_userName} to process.");
        }
        return userDeviceUd!;
    }


}