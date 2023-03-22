using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementApi.Models;

public class UserOrder
{

    /// <summary>
    /// Get or set the order ID of the user, where 0 indicates that the user is currently using this device.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int OrderId { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<UserDevice>? UserDevices { get; set; }
}