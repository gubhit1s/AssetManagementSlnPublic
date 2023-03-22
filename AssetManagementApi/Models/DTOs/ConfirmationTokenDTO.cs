namespace AssetManagementApi.Models.DTO;

public class ConfirmationTokenDTO
{

    public string? UserName { get; set; }

    public int DeviceId { get; set; }

    public int DeviceTypeId { get; set; }

    public Guid Token { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int TransferTypeId { get; set; }
}