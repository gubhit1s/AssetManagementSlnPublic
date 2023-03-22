using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetManagementApi.Models;

public class DeviceStatusType
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string StatusName { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Device>? Devices { get; set; }

    [JsonIgnore]
    public virtual ICollection<DeviceUnidentified>? DevicesUnidentified { get; set; }
}