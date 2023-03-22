namespace AssetManagementApi.Logics.TransferHandler.Decommission;

public interface IDecommissionHandler
{
    Task HandleDecommissionAsync(int deviceId);
}