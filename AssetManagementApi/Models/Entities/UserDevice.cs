using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetManagementApi.Models;

public class UserDevice
{
    [Key]
    [Column(Order = 0)]
    public Guid Id { get; set; }

    [Column(Order = 1)]
    [ForeignKey(nameof(Device))]
    public int DeviceId { get; set; }

    [JsonIgnore]
    public Device Device { get; set; } = null!;

    [Column(Order = 2)]
    public string? UserName { get; set; } = null!;

    [ForeignKey(nameof(UserOrder))]
    public int UserOrderId { get; set; }

    [JsonIgnore]
    public UserOrder? UserOrder { get; set; } = null!;

    [Column(Order = 3)]
    public DateTime FirstAssignedDate { get; set; }

}
