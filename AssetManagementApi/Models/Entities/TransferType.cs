using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AssetManagementApi.Models;

public class TransferType
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>
    /// Get the transfers of this transfer type
    /// </summary>
    public ICollection<Transfer>? Transfers;
    public ICollection<ConfirmationToken>? ConfirmationTokens;
    public ICollection<ConfirmationTokenUd>? ConfirmationTokenUds;
}