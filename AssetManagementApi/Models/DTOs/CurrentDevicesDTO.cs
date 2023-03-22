namespace AssetManagementApi.Models.DTO;

public class CurrentDevicesDTO
{

    public List<DeviceDTO>? CurrentDevicesIdentified { get; set; }

    public List<DeviceUnidentifiedDTO>? CurrentDevicesUnidentified { get; set; }
}
