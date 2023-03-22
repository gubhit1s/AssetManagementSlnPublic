using System.Text;
using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Logics.TransferHandler.Assignation;
using AssetManagementApi.Logics.TransferHandler.Withdrawal;
using AssetManagementApi.Logics.TransferHandler.Reassignation;
using AssetManagementApi.Logics.TransferHandler.Decommission;
using AssetManagementApi.Logics.TransferHandler.Repair;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Extensions;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace AssetManagementApi.Logics.TokenValidation;

public class TokenHandler : ITokenHandler
{
    private readonly AssetContext _assetContext;
    private readonly IAssignationHandler _assignationHandler;
    private readonly IWithdrawalHandler _withdrawalHandler;
    private readonly IReassignationHandler _reassignationHandler;
    private readonly IDecommissionHandler _decommissionHandler;
    private readonly IEmailHandler _emailHandler;
    private readonly RepairHandler _repairHandler;
    private readonly IAdUser _adUser;
    private readonly IConfiguration _configuration;

    public TokenHandler(
        AssetContext assetContext,
        IAssignationHandler assignationHandler,
        IWithdrawalHandler withdrawalHandler,
        IReassignationHandler reassignationHandler,
        IDecommissionHandler decommissionHandler,
        IEmailHandler emailHandler,
        RepairHandler repairHandler,
        IAdUser adUser,
        IConfiguration configuration
    )
    {
        _assetContext = assetContext;
        _assignationHandler = assignationHandler;
        _withdrawalHandler = withdrawalHandler;
        _decommissionHandler = decommissionHandler;
        _reassignationHandler = reassignationHandler;
        _emailHandler = emailHandler;
        _repairHandler = repairHandler;
        _adUser = adUser;
        _configuration = configuration;
    }

    public static int expirationHour = 24;
    public async Task<Guid> AddNewTokenToDbAsync(int deviceId, string userName, int transferTypeId)
    {
        Guid token = Guid.NewGuid();
        DateTime expiryDate = DateTime.Now.AddMinutes(Convert.ToInt32(_configuration["ConfirmationExpiryTimeInMinutes"]));

        //Delete the previous token if a request is sent more than once.
        var previousCt = await _assetContext.ConfirmationTokens.FirstOrDefaultAsync(ct =>
            ct.DeviceId == deviceId && ct.UserName == userName);
        if (previousCt is not null)
        {
            _assetContext.ConfirmationTokens.Remove(previousCt);
        }

        //Add new token to db
        ConfirmationToken ct = new ConfirmationToken()
        {
            Token = token,
            DeviceId = deviceId,
            UserName = userName,
            ExpiryDate = expiryDate,
            TransferTypeId = transferTypeId
        };
        await _assetContext.ConfirmationTokens.AddAsync(ct);
        await _assetContext.SaveChangesAsync();
        return token;
    }

    public async Task SendEmailConfirmationAsync(int deviceId, string userName, Guid token, int transferTypeId, string domainName)
    {
        User? user = _adUser.Users.FirstOrDefault(u => u.UserName == userName);
        if (user is null)
        {
            throw new ArgumentException("Can not find the user to send email");
        }
        Device device = await GetDeviceWithTypeNameAsync(deviceId);

        string urlAccept = $"{domainName}/#/device/confirm?token={token}&userConfirmed=true";
        string urlDecline = $"{domainName}/#/device/confirm?token={token}&userConfirmed=false";

        string emailBodyAssignHeader = $"<p>Dear <span><b>{user.DisplayName}</b></span>," +
            $"<p>The IT Department has requested to provide you a <span><b>{device.DeviceType?.Name}</span></b> with the following information:</p>";
        string emailBodyWithdrawHeader = $"<p>Dear <span><b>{user.DisplayName}</b></span>," +
            $"<p>The IT Department has requested to withdraw a <span><b>{device.DeviceType?.Name}</span></b> from you with the following information:</p>";
        string emailBodyReassignHeader = $"<p>Dear <span><b>{user.DisplayName}</b></span>," +
            $"<p>The IT Department has requested to replace a <span><b>{device.DeviceType?.Name}</span></b> of yours by another one with the following information:</p>";
        string emailBodyRepairHeader = $"<p>Dear <span><b>{user.DisplayName}</b></span>," +
            $"<p>The IT Department has requested to repair your current <span><b>{device.DeviceType?.Name}</span></b>. You are provided another one with the following information:</p>";

        string emailBody = "<ul>" +
                $"<li>Service Tag: <span><b>{device.ServiceTag}</span></b></li>" +
                $"<li>Device Name: <span><b>{device.DeviceName}</span></b></li>" +
                $"<li>Model Number: <span><b>{device.DeviceModel}</span></b></li>" +
            "</ul>" +
        "<p>Please help to check your device with the details above. If they are all correct, please click on the link below to give your confirmation:</p>" +
            $"<a href=\"{urlAccept}\" type=\"button\">Confirm the device information</a>" +
            $"<p>Otherwise, please click on the link below to reject this transaction and correct the information:</p>" +
            $"<a href=\"{urlDecline}\" type=\"button\">Reject and correct the device information</a>" +
            "<p><b><em>Note: </em></b><em>The links above will expire after 24 hours.</em>" +
            "<br><p>Thank you.</p>";

        //Send an email with the token generated.
        MailInfo mailConfig = new MailInfo()
        {
            To = user.Email!
        };

        mailConfig.HtmlBody = transferTypeId switch
        {
            2 => emailBodyAssignHeader + emailBody,
            3 => emailBodyReassignHeader + emailBody,
            4 => emailBodyWithdrawHeader + emailBody,
            6 => emailBodyRepairHeader + emailBody,
            _ => throw new ArgumentException("Something wrong happened while processing the email content, please contact IT support"),
        };

        mailConfig.Subject = transferTypeId switch
        {
            2 => $"Assignation: Device {device.ServiceTag}",
            3 => $"Reassignation: Device {device.ServiceTag}",
            4 => $"Withdrawal: Device {device.ServiceTag}",
            6 => $"Reassignation - maintenance: Device {device.ServiceTag}",
            _ => throw new ArgumentException("Something wrong happened"),
        };

        await _emailHandler.SendEmailAsync(mailConfig);
    }

    public async Task<bool> ValidateTokenAsync(Guid token, bool userConfirmed, string? declinedReason)
    {
        ConfirmationToken? ct = await _assetContext.FindAsync<ConfirmationToken>(token);
        if (ct is not null)
        {
            if (ct.ExpiryDate < DateTime.Now)
            {
                //Delete the token if expired.
                _assetContext.Remove(ct);
                await _assetContext.SaveChangesAsync();
                return false;
            }

            //delete the reminding job.
                string jobId = await _assetContext.DeleteJobIdAsync(token);
                BackgroundJob.Delete(jobId);
                
            //Save token changes before firing background jobs to avoid deadlocks.
            _assetContext.Remove(ct);
            await _assetContext.SaveChangesAsync();

            if (userConfirmed)
            {
                await HandleConfirmedTransferAsync(ct);
            }
            else
            {
                BackgroundJob.Enqueue(() => HandleDeclinedTransferAsync(ct, declinedReason));
            }


            return true;
        }
        return false;
    }

    public async Task HandleConfirmedTransferAsync(ConfirmationToken ct)
    {
        if (ct is null)
        {
            throw new ArgumentException("Cannot find the token.");
        }
        switch (ct.TransferTypeId)
        {
            case TransferTypes.Assignation:
                BackgroundJob.Enqueue<IAssignationHandler>((a) => a.HandleAssignationAsync(ct.DeviceId, ct.UserName!, ct.Token, true));
                break;
            case TransferTypes.Reassignation:
                BackgroundJob.Enqueue<IReassignationHandler>((r) => r.HandleReassignationAsync(ct.DeviceId, ct.UserName!, true, null));
                break;
            case TransferTypes.Withdrawal:
                BackgroundJob.Enqueue<IWithdrawalHandler>((w) => w.HandleWithdrawalAsync(ct.DeviceId, ct.UserName!, ct.Token, true));
                break;
            case 5:
                await _decommissionHandler.HandleDecommissionAsync(ct.DeviceId);
                break;
            case 6:
                BackgroundJob.Enqueue<RepairHandler>((r) => r.HandleRepairFromUser(ct.DeviceId, ct.UserName!, true));
                break;
            default:
                throw new ArgumentOutOfRangeException("Cannot find the proper task to execute");
        }
    }

    public async Task HandleDeclinedTransferAsync(ConfirmationToken ct, string? declinedReason)
    {
        User? user = _adUser.Users.FirstOrDefault(u => u.UserName == ct.UserName);
        if (user is null)
        {
            throw new ArgumentException("Can not find the user to send email");
        }
        Device device = await GetDeviceWithTypeNameAsync(ct.DeviceId);

        EmailAdministrator? email = await _assetContext.EmailAdministrators.FindAsync(2);
        if (email is null)
        {
            throw new ArgumentException("Cannot find the admin email to send");
        }

        await _emailHandler.SendEmailAsync(new MailInfo()
        {
            To = email.EmailAdmin,
            Subject = $"User {ct.UserName} has declined the transfer.",
            HtmlBody = $"<p>User {ct.UserName}({user.DisplayName}) has declined the transfer of the device {device.ServiceTag} with the following reason(s):</p>" +
                        $"<p>{declinedReason}</p><p>As a result, this transfer was not processed.</p>"
        });
    }

    public async Task HandleExpiredTokenAsync(ConfirmationTokenDTO ct)
    {
        await SendEmailReminderAsync(ct.DeviceId, ct.UserName!, ct.TransferTypeId);
        await DeleteExpiredTokenAsync(ct.Token);
    }

    public async Task DeleteExpiredTokenAsync(Guid token)
    {
        ConfirmationToken? ct = await _assetContext.ConfirmationTokens.FindAsync(token);
        if (ct is null)
        {
            throw new ArgumentException("Cannot find the expired token.");
        }
        _assetContext.ConfirmationTokens.Remove(ct);
        string jobId = await _assetContext.DeleteJobIdAsync(ct.Token);
        BackgroundJob.Delete(jobId);
        await _assetContext.SaveChangesAsync();
    }

    public async Task SendEmailReminderAsync(int deviceId, string userName, int transferTypeId)
    {
        TransferType? type = await _assetContext.TransferTypes.FindAsync(transferTypeId);
        if (type is null)
        {
            throw new ArgumentException("Cannot find the proper transfer type id to parse");
        }
        Device device = await GetDeviceWithTypeNameAsync(deviceId);

        string userDisplayName = _adUser.GetDisplayName(userName);
        string userEmail = _adUser.GetEmail(userName);

        StringBuilder body = new StringBuilder();
        body.Append($"<p>User <b>{userDisplayName}</b>({userEmail}) did not confirm the transfer. The confirmation url has expired. Please log in to the system and resend the email.");
        body.Append($"<p>Transfer details: <b>{type.Name}</b> of a <b>{device.DeviceType!.Name}</b> with service tag <b>{device.ServiceTag}</b>.");

        string adminEmail = await _assetContext.GetEmailAdminAsync();
        MailInfo mailInfo = new()
        {
            To = adminEmail,
            Subject = $"Confirmation Expired: {type.Name}",
            HtmlBody = body.ToString()
        };
        await _emailHandler.SendEmailAsync(mailInfo);
        await _assetContext.SaveChangesAsync();
    }

    public async Task<Device> GetDeviceWithTypeNameAsync(int deviceId)
    {

        Device? device = await _assetContext.Devices.Include(d => d.DeviceType).FirstOrDefaultAsync(d => d.Id == deviceId);
        if (device is null)
        {
            throw new ArgumentException($"Can not find information for device {deviceId}");
        }
        return device;
    }
}