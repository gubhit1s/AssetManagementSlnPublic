namespace AssetManagementApi.Logics.EmailHandler;

public interface IEmailHandler
{

    Task SendEmailAsync(MailInfo mailInfo);
}