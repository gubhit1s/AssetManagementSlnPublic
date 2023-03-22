namespace AssetManagementApi.Models.DTO;

public class DeviceOverviewDTO
{
    public int DeviceTypeId { get; set; }

    public string DeviceTypeName { get; set; } = null!;

    public int TotalAmount { get; set; } = 0;

    public int InStockAmount { get; set; } = 0;

    public int InUseAmount { get; set; } = 0;

    public int OtherAmount
    {
        get
        {
            return TotalAmount - InStockAmount - InUseAmount;
        }
    }
}