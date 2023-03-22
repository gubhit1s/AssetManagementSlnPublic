using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementApi.Models;

[Table("TransferHistory")]
public class Transfer
{

    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(TransferType))]
    public int TransferTypeId { get; set; }

    [ForeignKey(nameof(AssetManagementApi.Models.Device))]
    public int DeviceId { get; set; }

    //Foreign key already defined in AssetContext.
    public int TransferFromDestinationId { get; set; }

    public string? TransferFromUser { get; set; }

    //Foreign key already defined in AssetContext.
    public int TransferToDestinationId { get; set; }

    public string? TransferToUser { get; set; }

    public TransferDestination TransferFromDestination { get; set; } = null!;

    public TransferDestination TransferToDestination { get; set; } = null!;

    public DateTime TransferDate { get; set; }

    public virtual TransferType TransferTypeRef { get; set; } = null!;

    public virtual Device Device { get; set; } = null!;
}