using AssetManagementApi.Models;
using AssetManagementApi.Extensions;

namespace AssetManagementApi.Models.DTO;

public class DeviceDTO
{

    public int Id { get; set; }

    public string ServiceTag { get; set; } = null!;

    public string? DeviceName { get; set; }

    public string? AcquiredDate { get; set; }
    
    public string? DeviceModel { get; set; }

    public string? PONumber { get; set; }

    public int DeviceTypeId { get; set; }

    public string? DeviceTypeName { get; set; } = null!;

    public int DeviceStatusId { get; set; }

    public string? DeviceStatusName { get; set; } = null!;

    public string? CurrentUsername { get; set; }

    public string? CurrentUserEmail { get; set; }

    public string? LastUser1 { get; set; }

    public string? LastUser2 { get; set; }

    public static Device ToDevice(DeviceDTO deviceDTO)
    {
        DateTime.TryParse(deviceDTO.AcquiredDate, out DateTime wd);
        Device device = new Device()
        {
            Id = deviceDTO.Id,
            ServiceTag = deviceDTO.ServiceTag.NullIfWhiteSpace(),
            DeviceName = deviceDTO.DeviceName.NullIfWhiteSpace(),
            DeviceTypeId = deviceDTO.DeviceTypeId,
            DeviceStatusId = deviceDTO.DeviceStatusId, 
            AcquiredDate = wd == default(DateTime) ? null : wd,
            DeviceModel = deviceDTO.DeviceModel.NullIfWhiteSpace(),
            PONumber = deviceDTO.PONumber.NullIfWhiteSpace()
        };
        return device;
    }
    

}