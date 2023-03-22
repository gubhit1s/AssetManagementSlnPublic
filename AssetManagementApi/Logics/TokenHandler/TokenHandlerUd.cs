using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Logics.TransferHandler.Unidentified;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Hangfire;

namespace AssetManagementApi.Logics.TokenValidation;

public class TokenHandlerUd : ITokenHandler
{

    private readonly AssetContext _assetContext;
    private readonly UnidentifiedTransferHandler _unidentifiedTransferHandler;
    private readonly IAdUser _adUser;
    private readonly IEmailHandler _emailHandler;
    private readonly IConfiguration _configuration;

    public TokenHandlerUd(AssetContext assetContext, UnidentifiedTransferHandler unidentifiedTransferHandler, IAdUser adUser,
        IEmailHandler emailHandler, IConfiguration configuration)
    {
        _assetContext = assetContext;
        _unidentifiedTransferHandler = unidentifiedTransferHandler;
        _adUser = adUser;
        _emailHandler = emailHandler;
        _configuration = configuration;
    }

    public async Task<Guid> AddNewTokenToDbAsync(int deviceTypeId, string userName, int transferTypeId)
    {
        Guid token = Guid.NewGuid();
        DateTime expiryDate = DateTime.Now.AddMinutes(Convert.ToInt32(_configuration["ConfirmationExpiryTimeInMinutes"]));

        //Delete the previous token if a request is sent more than once.
        var previousCt = await _assetContext.ConfirmationTokens.FirstOrDefaultAsync(ct =>
            ct.DeviceId == deviceTypeId && ct.UserName == userName);
        ConfirmationTokenUd ct = new ConfirmationTokenUd()
        {
            Token = token,
            DeviceTypeId = deviceTypeId,
            UserName = userName,
            ExpiryDate = expiryDate,
            TransferTypeId = transferTypeId
        };
        await _assetContext.ConfirmationTokensUd.AddAsync(ct);
        await _assetContext.SaveChangesAsync();
        return token;
    }

    public async Task SendEmailConfirmationAsync(int deviceCode, string userName, Guid token, int transferTypeId, string domainName)
    {
        User? user = _adUser.Users.FirstOrDefault(u => u.UserName == userName);
        if (user is null)
        {
            throw new ArgumentException("Can not find the user to send email");
        }

        DeviceType? deviceType = await _assetContext.DeviceTypes.FindAsync(deviceCode);
        if (deviceType == null || deviceType.IsIdentified)
        {
            throw new ArgumentException("Device type was not found or not unidentified");
        }
        string deviceTypeName = deviceType.Name;

        string urlAccept = $"{domainName}/#/device/unidentified/confirm?token={token}&userConfirmed=true";
        string urlDecline = $"{domainName}/#/device/unidentified/confirm?token={token}&userConfirmed=false";


        string emailBodyAssignHeader = $"<p>Dear <span><b>{user.DisplayName}</b></span>," +
            $"<p>The IT Department has requested to assign to you a new <b>{deviceTypeName}</b>.</p>";
        string emailBodyWithdrawHeader = $"<p>Dear <span><b>{user.DisplayName}</b></span>," +
            $"<p>The IT Department has requested to withdraw a <b>{deviceTypeName}</b> from you.</p>";
        string emailBodyReassignHeader = $"<p>Dear <span><b>{user.DisplayName}</b></span>," +
            $"<p>The IT Department has requested to assign to you a new <b>{deviceTypeName}</b> in replacement of the old one.</p>";
        string emailBodyReassignDamagedHeader = $"<p>Dear <span><b>{user.DisplayName}</b></span>," +
            $"<p>The IT Department has requested to assign to you a new <b>{deviceTypeName}</b> in replacement of the damaged one.</p>";

        string emailBody = "<p>If this is correct, please click on the link below to give your confirmation:</p>" +
            $"<a href=\"{urlAccept}\" type=\"button\">Confirm the device information</a>" +
            $"<p>Otherwise, please click on the link below to reject this transaction and correct the information:</p>" +
            $"<a href=\"{urlDecline}\" type=\"button\">Reject and correct the device information</a>" +
            "<p><b><em>Note: </em></b><em>The links above will expire after 24 hours.</em>" +
            "<br><p>Thank you.</p>";

        MailInfo mailConfig = new MailInfo()
        {
            To = user.Email!
        };

        mailConfig.HtmlBody = transferTypeId switch
        {
            2 => emailBodyAssignHeader + emailBody,
            3 => emailBodyReassignHeader + emailBody,
            4 => emailBodyWithdrawHeader + emailBody,
            10 => emailBodyReassignDamagedHeader + emailBody,
            _ => throw new ArgumentException("Something wrong happened while processing the email content, please contact IT support."),
        };

        mailConfig.Subject = transferTypeId switch
        {
            2 => $"Assignation: {deviceTypeName}",
            3 => $"Reassignation: {deviceTypeName}",
            4 => $"Withdrawal: {deviceTypeName}",
            10 => $"Reassignation - Damaged: {deviceTypeName}",
            _ => throw new ArgumentException("Wrong transfer type to process."),
        };

        await _emailHandler.SendEmailAsync(mailConfig);
    }

    public async Task<bool> ValidateTokenAsync(Guid token, bool userConfirmed, string? declinedReason)
    {
        ConfirmationTokenUd? ct = await _assetContext.FindAsync<ConfirmationTokenUd>(token);
        if (ct is not null)
        {
            if (ct.ExpiryDate < DateTime.Now)
            {
                _assetContext.Remove(ct);
                await _assetContext.SaveChangesAsync();
                return false;
            }

            
                //delete the reminding job.
            string jobId = await _assetContext.DeleteJobIdAsync(token);
            BackgroundJob.Delete(jobId);
            
            _assetContext.Remove(ct);
            await _assetContext.SaveChangesAsync();

            if (userConfirmed)
            {
                BackgroundJob.Enqueue<UnidentifiedTransferHandler>((u) => u.HandleTransferUdDevicesInvolvingUserAsync(ct, true));
            }
            else
            {
                BackgroundJob.Enqueue(() => HandleDeclinedTransferAsync(ct, declinedReason));
            }


            return true;
        }
        return false;
    }

    public async Task HandleDeclinedTransferAsync(ConfirmationTokenUd ct, string? declinedRason)
    {
        User? user = _adUser.Users.FirstOrDefault(u => u.UserName == ct.UserName);
        if (user is null)
        {
            throw new ArgumentException("Can not find the user to send email");
        }
        DeviceType? deviceType = await _assetContext.DeviceTypes.FindAsync(ct.DeviceTypeId);
        if (deviceType is null)
        {
            throw new ArgumentException($"Can not find information for device type {ct.DeviceTypeId}");
        }

        EmailAdministrator? email = await _assetContext.EmailAdministrators.FindAsync(2);
        if (email is null)
        {
            throw new ArgumentException("Cannot find the admin email to send");
        }

        await _emailHandler.SendEmailAsync(new MailInfo()
        {
            To = email.EmailAdmin,
            Subject = $"User {ct.UserName} has declined the transfer.",
            HtmlBody = $"<p>User {ct.UserName}({user.DisplayName}) has declined the transfer of a <b>{deviceType.Name}</b> with the following reason(s):</p>" +
                        $"<p>{declinedRason}</p><p>As a result, this transfer was not processed.</p>"
        });
    }

    public async Task HandleExpiredTokenAsync(ConfirmationTokenDTO ctUd)
    {
        
        await SendEmailReminderAsync(ctUd.DeviceTypeId, ctUd.UserName!, ctUd.TransferTypeId);
        await DeleteExpiredTokenAsync(ctUd.Token);
    }

    public async Task DeleteExpiredTokenAsync(Guid token)
    {
        ConfirmationTokenUd? ct = await _assetContext.ConfirmationTokensUd.FindAsync(token);
        if (ct is null)
        {
            throw new ArgumentException("Cannot find the expired token.");
        }
        _assetContext.ConfirmationTokensUd.Remove(ct);

        string jobId = await _assetContext.DeleteJobIdAsync(ct.Token);
        BackgroundJob.Delete(jobId);

        await _assetContext.SaveChangesAsync();
    }

    public async Task SendEmailReminderAsync(int deviceTypeId, string userName, int transferTypeId)
    {
        TransferType? type = await _assetContext.TransferTypes.FindAsync(transferTypeId);
        if (type is null)
        {
            throw new ArgumentException("Cannot find the proper transfer type id to parse");
        }
        User? user = _adUser.Users.FirstOrDefault(u => u.UserName == userName);
        if (user is null)
        {
            throw new ArgumentException("Can not find the user to send email");
        }
        DeviceType? deviceType = await _assetContext.DeviceTypes.FindAsync(deviceTypeId);
        if (deviceType == null || deviceType.IsIdentified)
        {
            throw new ArgumentException("Device type was not found or not unidentified");
        }

        string userDisplayName = _adUser.GetDisplayName(userName);
        string userEmail = _adUser.GetEmail(userName);

        StringBuilder body = new StringBuilder();
        body.Append($"<p>User <b>{userDisplayName}</b>({userEmail}) did not confirm the transfer. The confirmation url has expired. Please log in to the system and resend the email.</p>");
        body.Append($"<p>Transfer details: <b>{type.Name}</b> of a <b>{deviceType.Name}</b>.");

        string adminEmail = await _assetContext.GetEmailAdminAsync();
        MailInfo mailInfo = new()
        {
            To = adminEmail,
            Subject = $"Confirmation Expired: {type.Name}",
            HtmlBody = body.ToString()
        };
        await _emailHandler.SendEmailAsync(mailInfo);
    }

}
