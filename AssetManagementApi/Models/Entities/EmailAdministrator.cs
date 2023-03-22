using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementApi.Models;

public class EmailAdministrator
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [RegularExpression(@"^[_A-Za-z0-9.]+\@tmf-group\.com$", ErrorMessage = "Email must be a TMF Group email.")]
    public string EmailAdmin { get; set; } = null!;

    
    [Required]
    public string SmtpHost { get; set; } = null!;

    [Required]
    public int SmtpPort { get; set; }

    [Required]
    public bool EnableSSL { get; set; }

    [Required]
    [RegularExpression(@"^[_A-Za-z0-9.]+\@tmf-group\.com$", ErrorMessage = "Email must be a TMF Group email.")]
    public string EmailFrom { get; set; } = null!;

    [Required]
    public string TitleFrom { get; set; } = null!;

}