namespace AssetManagementApi.Models.DTO;

public class CurrentUserCartDTO
{

    public string? UserName { get; set; }
    public List<int> DeviceTypeIds { get; set; } = new List<int>();
}