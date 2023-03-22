using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Logging;
using AssetManagementApi.Models;

namespace AssetManagementApi.Logics.EmailHandler;
public class EmailHandler : IEmailHandler
{

    private readonly ILogger<EmailHandler> _logger;
    private readonly AssetContext _assetContext;

    public EmailHandler(ILogger<EmailHandler> logger, AssetContext assetContext)
    {
        _logger = logger;
        _assetContext = assetContext;
    }

    private MailMessage PrepareMailAsync(MailInfo mailInfo, EmailAdministrator emailSetting)
    {
        mailInfo.From = emailSetting.EmailFrom;
        mailInfo.FromAs = emailSetting.TitleFrom;

        return new MailMessage(new MailAddress(mailInfo.From, mailInfo.FromAs), new MailAddress(mailInfo.To))
        {
            Subject = mailInfo.Subject,
            IsBodyHtml = true,
            Body = mailInfo.HtmlBody
        };
    }

    public async Task SendEmailAsync(MailInfo mailInfo)
    {
        EmailAdministrator? setting = await GetEmailSettingAsync();

        SmtpClient smtpClient = new SmtpClient()
        {
            Host = setting.SmtpHost,
            Port = setting.SmtpPort,
            EnableSsl = setting.EnableSSL
        };

        try
        {
            MailMessage preparedMessage = PrepareMailAsync(mailInfo, setting!);
            await smtpClient.SendMailAsync(preparedMessage);
            _logger.LogInformation($"Email has been sent to {mailInfo.To}");
        } catch (SmtpFailedRecipientException ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public async Task<EmailAdministrator> GetEmailSettingAsync()
    {
        EmailAdministrator? setting = await _assetContext.EmailAdministrators.FindAsync(2);
        if (setting == null)
        {
            throw new ArgumentException("Cannot find the email setting to process!");
        }
        return setting;
    }

}

