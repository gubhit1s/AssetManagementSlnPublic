namespace AssetManagementApi.Models.DTO;

public class CompletedTransferDTO
{
    public DateTime TransferDate { get; set; }

    public string? DeviceServiceTag { get; set; }
    
    public string? DeviceTypeName { get; set; }

    public string? TransferFrom { get; set; } = null!;

    public string? TransferTo { get; set; } = null!;

    public int? Amount { get; set; }

    public string? TransferTypeName { get; set; } = null!;
}
