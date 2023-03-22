namespace AssetManagementApi.Models.Reports;

public class DeviceUdReport
{

    public string DeviceType { get; set; } = null!;

    public string DeviceStatus { get; set; } = null!;
    
    public int Amount { get; set; }
}