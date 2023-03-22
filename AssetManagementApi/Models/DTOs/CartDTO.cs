namespace AssetManagementApi.Models.DTO;

public class CartDTO
{
    public int DeviceTypeId { get; set; }

    public int? DeviceId { get; set; }

    public string UserName { get; set; } = null!;

    public int TransferTypeId { get; set; }
    
}