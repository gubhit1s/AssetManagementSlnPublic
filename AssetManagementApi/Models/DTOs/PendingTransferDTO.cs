namespace AssetManagementApi.Models.DTO;

public class PendingTransferDTO
{

    public string? ServiceTag { get; set; }

    public string? DeviceTypeName { get; set; }

    public string TransferTypeName { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public DateTime? ExpiryDate { get; set; }

    public Guid Token { get; set; }
}