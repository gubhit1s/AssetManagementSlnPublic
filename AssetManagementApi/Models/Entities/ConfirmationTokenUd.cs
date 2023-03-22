using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetManagementApi.Models;

[Table("ConfirmationTokensUd")]
public class ConfirmationTokenUd
{

    public string? UserName { get; set; } = null!;

    [ForeignKey(nameof(AssetManagementApi.Models.DeviceType))]
    public int DeviceTypeId { get; set; }

    public DeviceType? DeviceType { get; set; }

    [Key]
    public Guid Token { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [ForeignKey(nameof(AssetManagementApi.Models.TransferType))]
    public int TransferTypeId { get; set; }

    public TransferType? TransferType { get; set; }

    public Guid? BatchId { get; set; }

    [NotMapped]
    public bool UserConfirmed { get; set; }

    [NotMapped]
    public string? DeclinedReason { get; set; }

}