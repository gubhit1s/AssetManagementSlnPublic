using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AssetManagementApi.Logics.TokenValidation;
using System.Text.Json.Serialization;

namespace AssetManagementApi.Models;

public class ConfirmationToken
{

    public string? UserName { get; set; } = null!;

    [ForeignKey(nameof(AssetManagementApi.Models.Device))]
    public int DeviceId { get; set; }

    [JsonIgnore]
    public Device? Device { get; set; }

    [Key]
    public Guid Token { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [ForeignKey(nameof(AssetManagementApi.Models.TransferType))]
    public int TransferTypeId { get; set; }

    [JsonIgnore]
    public TransferType? TransferType { get; set; }

    [NotMapped]
    public bool UserConfirmed { get; set; }

    [NotMapped]
    public string? DeclinedReason { get; set; }

    public Guid? BatchId { get; set; }


}