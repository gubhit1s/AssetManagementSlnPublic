using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AssetManagementApi.Models.DTO;

namespace AssetManagementApi.Models;

[NotMapped]
public class User
{

    public string? UserName { get; set; } = null!;

    public string? DisplayName { get; set; } = null!;

    public string? Email { get; set; } = null!;

    public string? Department { get; set; }

    public string? Office { get; set; }

    public DateTime? ServiceStartDate { get; set; }

    public DateTime? ResignationDate { get; set; }

    public virtual ICollection<DeviceDTO>? Devices { get; set; }

    public virtual ICollection<DeviceType>? DevicesUnidentified { get; set; }


    //    public virtual ICollection<Transfer>? TransfersFromUser { get; set; }

    //    public virtual ICollection<Transfer>? TransfersToUser { get; set; } 
}