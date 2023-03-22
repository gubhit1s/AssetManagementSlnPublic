using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementApi.Models;

public class TransferDestination
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string Destination { get; set; } = null!;

    public ICollection<Transfer>? TransfersFromDestination;

    public ICollection<Transfer>? TransfersToDestination;

    public ICollection<TransferUd>? TransfersFromDestinationUd;

    public ICollection<TransferUd>? TransfersToDestinationUd;
}