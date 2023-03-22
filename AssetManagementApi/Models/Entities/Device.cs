using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetManagementApi.Models;

[Table("DevicesIdentified")]
public class Device
{

    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(AssetManagementApi.Models.DeviceType))]
    public int DeviceTypeId { get; set; }

    public string ServiceTag { get; set; } = null!;

    public string? DeviceName { get; set; }

    public string? DeviceModel { get; set; }

    public string? PONumber { get; set; }

    [ForeignKey(nameof(DeviceStatusType))]
    public int DeviceStatusId { get; set; }

    public DateTime? AcquiredDate { get; set; }

    public virtual DeviceType? DeviceType { get; set; } = null!;

    public virtual DeviceStatusType? DeviceStatus { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<UserDevice>? UserDevices { get; set; }

    [JsonIgnore]
    public virtual ICollection<Transfer>? Transfers { get; set; }

    [JsonIgnore]
    public virtual ICollection<ConfirmationToken>? ConfirmationTokens { get; set; }

}

