using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Logics.TransferHandler.Bulk;
using AssetManagementApi.Extensions;
using AssetManagementApi.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Hangfire;

namespace AssetManagementApi.Logics.TokenValidation;

public class BulkTokenHandler : IBulkTokenHandler
{

    private readonly AssetContext _assetContext;
    private readonly IAdUser _adUser;
    private readonly IEmailHandler _emailHandler;
    private readonly IBulkHandler _bulkHandler;
    private readonly IConfiguration _configuration;

    private readonly BulkTransferHelper _bulkHelper;

    public BulkTokenHandler(AssetContext assetContext,
        IAdUser adUser,
        IEmailHandler emailHandler,
        IBulkHandler bulkHandler,
        BulkTransferHelper bulkHelper,
        IConfiguration configuration
    )
    {
        _assetContext = assetContext;
        _adUser = adUser;
        _emailHandler = emailHandler;
        _bulkHandler = bulkHandler;
        _bulkHelper = bulkHelper;
        _configuration = configuration;
    }

    public async Task<Guid> AddNewTokenToDbAsync(List<CartDTO> carts)
    {
        Guid batchId = Guid.NewGuid();
        //A transfer is considered unidentified if the deviceId is null.
        foreach (CartDTO cart in carts)
        {
            if (cart.DeviceId != null) //identified
            {
                int deviceId = (int)cart.DeviceId;
                DeleteAllPreviousPendingTokens(cart.UserName!);
                ConfirmationToken ct = new ConfirmationToken()
                {
                    Token = Guid.NewGuid(),
                    DeviceId = deviceId,
                    TransferTypeId = cart.TransferTypeId,
                    BatchId = batchId,
                    ExpiryDate = DateTime.Now.AddMinutes(Convert.ToInt32(_configuration["ConfirmationExpiryTimeInMinutes"])),
                    UserName = cart.UserName
                };
                await _assetContext.ConfirmationTokens.AddAsync(ct);
            }
            else
            {
                int deviceTypeId = cart.DeviceTypeId;
                DeleteAllPreviousPendingTokens(cart.UserName!);
                ConfirmationTokenUd ctUd = new ConfirmationTokenUd()
                {
                    Token = Guid.NewGuid(),
                    DeviceTypeId = deviceTypeId,
                    TransferTypeId = cart.TransferTypeId,
                    BatchId = batchId,
                    ExpiryDate = DateTime.Now.AddMinutes(Convert.ToInt32(_configuration["ConfirmationExpiryTimeInMinutes"])),
                    UserName = cart.UserName
                };
                await _assetContext.ConfirmationTokensUd.AddAsync(ctUd);
            }
        }

        await _assetContext.SaveChangesAsync();
        return batchId;
    }

    public void DeleteAllPreviousPendingTokens(string userName)
    {
        var cts = _assetContext.ConfirmationTokens.Where(ct => ct.UserName == userName);
        if (cts is not null)
        {
            _assetContext.ConfirmationTokens.RemoveRange(cts);
        }

        var ctUds = _assetContext.ConfirmationTokensUd.Where(ct => ct.UserName == userName);
        if (ctUds is not null)
        {
            _assetContext.ConfirmationTokensUd.RemoveRange(ctUds);
        }

    }

    public async Task SendEmailConfirmationAsync(List<CartDTO> carts, Guid batchId, string domainName)
    {
        string urlAccept = $"{domainName}/#/device/bulk/confirm?token={batchId}&userConfirmed=true";
        string urlDecline = $"{domainName}/#/device/bulk/confirm?token={batchId}&userConfirmed=false";

        string userName = carts[0].UserName;
        string displayName = _adUser.GetDisplayName(userName);
        string email = _adUser.GetEmail(userName);
        string actionOfIt = carts[0].TransferTypeId == 4 ? "withdraw from" : "assign to";

        StringBuilder emailBody = new StringBuilder();
        emailBody.Append($"<p>Dear <b>{displayName}</b>,</p>" +
            $"<p>The IT Department has requested to {actionOfIt} you <b>{carts.Count}</b> devices:</p>");

        emailBody.Append(await _bulkHelper.GenerateCartDetailsAsync(carts));

        emailBody.Append("<p>Please check your device(s) with the details above. If they are all correct, please click on the link below to give your confirmation:</p>" +
            $"<a href=\"{urlAccept}\" type=\"button\">Confirm the device information</a>" +
            $"<p>Otherwise, please click on the link below to reject this transaction and correct the information:</p>" +
            $"<a href=\"{urlDecline}\" type=\"button\">Reject and correct the device information</a>" +
            "<p><b><em>Note: </em></b><em>The links above will expire after 24 hours.</em>" +
            "<br><p>Thank you.</p>");

        MailInfo mailInfo = new MailInfo()
        {
            To = email,
            Subject = carts[0].TransferTypeId == 4 ? "Asset Withdrawal" : "Asset Assignation",
            HtmlBody = emailBody.ToString()
        };

        await _emailHandler.SendEmailAsync(mailInfo);
    }

    public async Task<bool> ValidateTokenAsync(Guid token, bool userConfirmed, string? declinedReason)
    {
        var confirmationTokens = await _assetContext.ConfirmationTokens.AsNoTracking().Where(ct => ct.BatchId == token).ToListAsync();
        var confirmationTokensUd = await _assetContext.ConfirmationTokensUd.AsNoTracking().Where(ct => ct.BatchId == token).ToListAsync();
        if (confirmationTokens.Count == 0 && confirmationTokensUd.Count == 0) return false;
        string userName = "";

        if (confirmationTokens.Count() > 0)
        {
            //Delete records if expiry date is overdue.
            if (confirmationTokens[0].ExpiryDate < DateTime.Now)
            {
                _assetContext.ConfirmationTokens.RemoveRange(confirmationTokens);
                await _assetContext.SaveChangesAsync();
                return false;
            }
            userName = confirmationTokens[0].UserName!;
        }
        if (confirmationTokensUd.Count() > 0)
        {
            //Delete records if expiry date is overdue.
            if (confirmationTokensUd[0].ExpiryDate < DateTime.Now)
            {
                _assetContext.ConfirmationTokensUd.RemoveRange(confirmationTokensUd);
                await _assetContext.SaveChangesAsync();
                return false;
            }
            userName = confirmationTokensUd[0].UserName!;
        }

        _assetContext.ConfirmationTokens.RemoveRange(confirmationTokens);
        _assetContext.ConfirmationTokensUd.RemoveRange(confirmationTokensUd);
        await _assetContext.SaveChangesAsync();
        
        //delete the reminding job.
        string jobId = await _assetContext.DeleteJobIdAsync(token);
        BackgroundJob.Delete(jobId);

        if (!userConfirmed)
        {
            int totalPrDevices = confirmationTokens.Count + confirmationTokensUd.Count;
            BackgroundJob.Enqueue(() => HandleDeclinedTransfersAsync(totalPrDevices, userName, declinedReason));
        } else
        {
            BackgroundJob.Enqueue<BulkHandler>((b) => b.HandleMultipleTransfersForBothCategories(confirmationTokens, confirmationTokensUd, userName));
        }

        return true;

    }

    public async Task HandleDeclinedTransfersAsync(int deviceCount, string userName, string? declinedReason = "")
    {
        string displayName = _adUser.GetDisplayName(userName);

        EmailAdministrator? email = await _assetContext.EmailAdministrators.FindAsync(2);
        if (email is null)
        {
            throw new ArgumentException("Cannot find the admin email to send");
        }

        await _emailHandler.SendEmailAsync(new MailInfo()
        {
            To = email.EmailAdmin!,
            Subject = $"User {userName} has declined the bulk transfers",
            HtmlBody = $"<p>User {userName}({displayName}) has declined the bulk transfer of <b>{deviceCount}</b> devices with the following reason(s):</p>" +
                        $"<p>{declinedReason}</p><p>As a result, this transfer was not processed.</p>"
        });
    }

    public async Task HandleExpiredTokenAsync(List<CartDTO> carts, Guid batchId)
    {
        await SendEmailReminderAsync(carts);
        await DeleteExpiredTokenAsync(batchId);
    }

    public async Task DeleteExpiredTokenAsync(Guid batchId)
    {
        var confirmationToken = _assetContext.ConfirmationTokens.Where(ct => ct.BatchId == batchId);
        var confirmationTokenUd = _assetContext.ConfirmationTokensUd.Where(ctU => ctU.BatchId == batchId);
        _assetContext.ConfirmationTokens.RemoveRange(confirmationToken);
        _assetContext.ConfirmationTokensUd.RemoveRange(confirmationTokenUd);
        
        string jobId = await _assetContext.DeleteJobIdAsync(batchId);
        BackgroundJob.Delete(jobId);
        await _assetContext.SaveChangesAsync();
    }

    public async Task SendEmailReminderAsync(List<CartDTO> carts)
    {
        string userName = carts[0].UserName;
        string userDisplayName = _adUser.GetDisplayName(userName);
        string userEmail = _adUser.GetEmail(userName);

        StringBuilder body = new StringBuilder();
        body.Append($"<p>User <b>{userDisplayName}</b>({userEmail}) did not confirm the cart transfer. The confirmation url has expired. Please log in to the system and resend the email.</p>");
        body.Append($"<p>Cart Details:</p>");
        string transferDetails = await _bulkHelper.GenerateCartDetailsAsync(carts);
        body.Append(transferDetails);

        string adminEmail = await _assetContext.GetEmailAdminAsync();

        MailInfo mailInfo = new()
        {
            To = adminEmail,
            Subject = "Confirmation Expired: Bulk Transfer",
            HtmlBody = body.ToString(),
        };

        await _emailHandler.SendEmailAsync(mailInfo);
    }

}
