namespace AssetManagementApi.Logics.TransferHandler.Decommission;

public interface IDecommissionPrepHandler
{
    public Task HandleDecommissionPrepAsync(int deviceId);
    public Task HandleRestockAsync(int DeviceId);
}