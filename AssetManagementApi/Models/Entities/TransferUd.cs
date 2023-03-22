using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;

[Table("TransferHistoryUd")]
public class TransferUd
{

    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(TransferType))]
    public int TransferTypeId { get; set; }

    [ForeignKey(nameof(AssetManagementApi.Models.DeviceType))]
    public int DeviceTypeId { get; set; }

    public int Amount { get; set; }

    public int TransferFromDestinationId { get; set; }

    public string? TransferFromUser { get; set; }

    public int TransferToDestinationId { get; set; }

    public string? TransferToUser { get; set; }

    //Foreign key already defined in AssetContext.
    public TransferDestination TransferFromDestination { get; set; } = null!;

    //Foreign key already defined in AssetContext.
    public TransferDestination TransferToDestination { get; set; } = null!;

    public DateTime TransferDate { get; set; }

    public virtual TransferType TransferTypeRef { get; set; } = null!;

    public virtual DeviceType DeviceType { get; set; } = null!;
}