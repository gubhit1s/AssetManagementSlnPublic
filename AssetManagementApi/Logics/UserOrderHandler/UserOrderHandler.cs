using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Models;
using AssetManagementApi.Models.AdUserGeneration;

namespace AssetManagementApi.Logics.UserOrderHandler;

public class UserOrderHandler : IUserOrderHandler
{

    private readonly AssetContext _assetContext;
    private readonly IAdUser _adUser;

    public UserOrderHandler(AssetContext assetContext, IAdUser adUser)
    {
        _assetContext = assetContext;
        _adUser = adUser;
    }

    public static (string? name, string? email) GetUserOfDeviceByOrder(AssetContext context, IAdUser adUser, int deviceId, int order)
    {
        var userNameByOrder = context.UserDevices.FirstOrDefault(ud => ud.DeviceId == deviceId && ud.UserOrderId == order);
        string? userName = null;
        string? userEmail = null;
        if (userNameByOrder is not null)
        {
            userName = userNameByOrder.UserName;
            userEmail = adUser.GetEmail(userName!);
        }
        return (userName, userEmail);
    }
    public async Task<string?> GetUserOfDeviceByOrderAsync(int deviceId, int order)
    {
        var userNameByOrder = await _assetContext.UserDevices.FirstOrDefaultAsync(ud => ud.DeviceId == deviceId && ud.UserOrderId == order);
        if (userNameByOrder is not null)
        {
            return userNameByOrder.UserName;
        }
        else
        {
            return null;
        }
    }
}