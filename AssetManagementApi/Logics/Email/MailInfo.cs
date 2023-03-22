namespace AssetManagementApi.Logics.EmailHandler;

public class MailInfo
{

    public string From { get; set; } = null!;

    public string FromAs { get; set; } = null!;

    public string To { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;
}