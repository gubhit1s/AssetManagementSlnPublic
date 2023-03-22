using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Extensions;
using System.Text;

namespace AssetManagementApi.Helpers;

public class BulkTransferHelper
{

    private readonly AssetContext _assetContext;

    public BulkTransferHelper(AssetContext assetContext)
    {
        _assetContext = assetContext;
    }

    public async Task<String> GenerateCartDetailsAsync(List<CartDTO> carts)
    {
        StringBuilder result = new StringBuilder();
        result.Append("<ol>");
        foreach (CartDTO cart in carts)
        {
            string deviceTypeName = await _assetContext.GetDeviceTypeNameAsync(cart.DeviceTypeId);
            result.Append($"<li><b>{deviceTypeName}</b>");

            result.Append("<ul>");

            if (await _assetContext.IsIdentified(cart.DeviceTypeId) && cart.DeviceId != null)
            {
                Device device = await _assetContext.GetIdentifiedDevice((int)cart.DeviceId);
                result.Append(
                $"<li>Service Tag: <span><b>{device.ServiceTag}</span></b></li>" +
                $"<li>Device Name: <span><b>{device.DeviceName}</span></b></li>" +
                $"<li>Model Number: <span><b>{device.DeviceModel}</span></b></li>"
                );
            }
            
            string transferType = await _assetContext.GetTransferTypeNameAsync(cart.TransferTypeId);
            result.Append($"<li>Transfer Type: <span><b>{transferType}</span></b></li>");
            
            result.Append("</ul>");
            result.Append("</li>");
        }
        result.Append("</ol>");
        return result.ToString();
    }

    public async Task<List<CartDTO>> TokensToCartDTOAsync(List<ConfirmationToken> cts, List<ConfirmationTokenUd> ctUds)
    {
        List<CartDTO> results = new List<CartDTO>();
        foreach (ConfirmationToken ct in cts)
        {
            CartDTO cart = new CartDTO()
            {
                DeviceId = ct.DeviceId,
                DeviceTypeId = await _assetContext.GetDeviceTypeIdByDeviceId(ct.DeviceId),
                UserName = ct.UserName!,
                TransferTypeId = ct.TransferTypeId
            };
            results.Add(cart);
        }

        foreach (ConfirmationTokenUd ct in ctUds)
        {
            CartDTO cart = new CartDTO()
            {
                DeviceTypeId = ct.DeviceTypeId,
                UserName = ct.UserName!,
                TransferTypeId = ct.TransferTypeId
            };
            results.Add(cart);
        }

        return results;
    }

    public async Task<string> GetExpiredCartDetails(List<CartDTO> carts)
    {
        StringBuilder bodySb = new StringBuilder();
        bodySb.Append("<table>");
        bodySb.Append("<th>Transfer Type</th>");
        bodySb.Append("<th>Device Type</th>");
        bodySb.Append("<th>Service Tag</th>");
        bodySb.Append("<th>Device Name</th>");

        foreach (CartDTO cart in carts)
        {
            bodySb.Append("<tr>");

            string? type = Enum.GetName(typeof(TransferType), cart.TransferTypeId);
            if (type == null) throw new ArgumentException("Cannot find transfer type");
            bodySb.Append($"<td>{type}</td>");

            string deviceTypeName = await _assetContext.GetDeviceTypeNameAsync(cart.DeviceTypeId);
            bodySb.Append($"<td>{deviceTypeName}</td>");

            if (await _assetContext.IsIdentified(cart.DeviceTypeId) && cart.DeviceId != null)
            {
                Device device = await _assetContext.GetIdentifiedDevice((int)cart.DeviceId);
                bodySb.Append($"<td>{device.ServiceTag}</td>");
                bodySb.Append($"<td>{device.DeviceName}</td>");
            }

            bodySb.Append("</tr>");
        }
        bodySb.Append("</table>");
        return bodySb.ToString();
    }

}
