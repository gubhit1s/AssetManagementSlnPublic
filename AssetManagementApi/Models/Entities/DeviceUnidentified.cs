using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetManagementApi.Models;

[Table("DevicesUnidentified")]
public class DeviceUnidentified
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(AssetManagementApi.Models.DeviceType))]
    public int DeviceTypeId { get; set; }

    [ForeignKey(nameof(AssetManagementApi.Models.DeviceStatusType))]
    public int DeviceStatusId { get; set; }

    public int Amount { get; set; } = 0;

    public virtual DeviceType? DeviceType { get; set; }
    
    public virtual DeviceStatusType? DeviceStatusType { get; set; }
}