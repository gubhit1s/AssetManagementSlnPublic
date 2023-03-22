namespace AssetManagementApi.Models.DTO;

public class DeviceUnidentifiedDTO
{
    public int DeviceTypeId { get; set; }

    public string? DeviceTypeName { get; set; }

    public int DeviceStatusId { get; set; }

    public string? DeviceStatusName { get; set; }

    public int Amount { get; set; } = 0;

    public int TransferTypeId { get; set; } = 0;

    public string? UserName { get; set; }
}