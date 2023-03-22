using AssetManagementApi.Models;
using AssetManagementApi.Models.AdUserGeneration;

namespace AssetManagementApi.Logics.UserOrderHandler;

public interface IUserOrderHandler
{
    Task<string?> GetUserOfDeviceByOrderAsync(int deviceId, int order);

}