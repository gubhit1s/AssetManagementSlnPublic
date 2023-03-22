using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AssetManagementApi.Models;

public class DeviceType
{

    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public bool IsIdentified { get; set; }

    [JsonIgnore]
    public virtual ICollection<DeviceUnidentified>? DevicesUnidentified { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Device>? Devices { get; set; }

    [JsonIgnore]
    public virtual ICollection<ConfirmationTokenUd>? ConfirmationTokenUds { get; set; }

}