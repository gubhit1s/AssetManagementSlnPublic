using AssetManagementApi.Models;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Logics.EmailHandler;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Logics.TokenValidation;

public class BaseTokenHandler
{

    private readonly AssetContext _assetContext;
    private readonly IAdUser _adUser;

    public BaseTokenHandler(AssetContext assetContext, IAdUser adUser)
    {
        _assetContext = assetContext;
        _adUser = adUser;
    }

}

