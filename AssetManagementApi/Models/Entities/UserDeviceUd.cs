using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetManagementApi.Models;

[Table("UserDevicesUd")]
public class UserDeviceUd
{

    [Key]
    [Column(Order = 0)]
    public Guid Id { get; set; }

    [ForeignKey(nameof(DeviceType))]
    [Column(Order = 1)]
    public int DeviceTypeId { get; set; }

    public DeviceType DeviceType { get; set; } = null!;

    [Column(Order = 2)]
    public string? UserName { get; set; }

    [Column(Order = 3)]
    public DateTime FirstAssignedDate { get; set; }
}



