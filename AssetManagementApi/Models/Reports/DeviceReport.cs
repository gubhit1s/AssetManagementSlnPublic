namespace AssetManagementApi.Models.Reports;

public class DeviceReport
{

    public int Id { get; set; }

    public string? DeviceType { get; set; } = null!;

    public string ServiceTag { get; set; } = null!;

    public string? DeviceName { get; set; }

    public string? AcquiredDate { get; set; }

    public string? DeviceModel { get; set; }

    public string? PONumber { get; set; }


    public string? DeviceStatus { get; set; } = null!;

    public string? CurrentUser { get; set; }

    public string? CurrentUserEmail { get; set; }

    public string? LastUser1 { get; set; }

    public string? LastUser1Email { get; set; }

    public string? LastUser2 { get; set; }

    public string? LastUser2Email { get; set; }
}