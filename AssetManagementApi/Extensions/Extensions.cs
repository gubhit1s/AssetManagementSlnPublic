using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AssetManagementApi.Extensions;
public static class Extensions
{
    public static string NullIfWhiteSpace(this string? input)
    {
        if (String.IsNullOrWhiteSpace(input))
        {
            return null!;
        }
        return input;
    }

    public static DateTime? NullIfDefaultDate(this DateTime? input)
    {
        if (input == default(DateTime))
        {
            return null;
        }
        return input;
    }

    public static string NullableDateToString(this DateTime? input) =>
        input != null ? ((DateTime)input).ToString("yyyy-MM-dd") : "";

    public async static Task<int> GetDeviceTypeIdByDeviceId(this AssetContext assetContext, int deviceId)
    {
        var result = await assetContext.Devices.FindAsync(deviceId);
        if (result == null) throw new ArgumentNullException("Cannot find the device with given id.");
        return result.DeviceTypeId;
    }

    public static async Task<string> GetDeviceTypeNameAsync(this AssetContext assetContext, int deviceTypeId)
    {
        DeviceType? deviceType = await assetContext.DeviceTypes.FindAsync(deviceTypeId);
        if (deviceType is null)
        {
            return "";
        }
        return deviceType.Name;
    }


    public static async Task<bool> IsIdentified(this AssetContext assetContext, int deviceTypeId)
    {
        DeviceType? deviceType = await assetContext.DeviceTypes.FindAsync(deviceTypeId);
        if (deviceType == null)
        {
            throw new ArgumentException("Cannot find Device Type");
        }
        return deviceType.IsIdentified;
    }

    public static async Task<Device> GetIdentifiedDevice(this AssetContext assetContext, int deviceId)
    {
        Device? device = await assetContext.Devices.FindAsync(deviceId);
        if (device == null)
        {
            throw new ArgumentException("Cannot find device");
        }
        return device;
    }

    public static async Task<string> GetEmailAdminAsync(this AssetContext assetContext)
    {
        EmailAdministrator? email = await assetContext.EmailAdministrators.FindAsync(2);
        if (email == null)
        {
            throw new ArgumentException("Cannot find admin email to send");
        }
        return email.EmailAdmin;
    }

    public static async Task<string> DeleteJobIdAsync(this AssetContext assetContext, Guid token)
    {
        ScheduledBackgroundJob? bgJob = await assetContext.ScheduledBackgroundJobs.FirstOrDefaultAsync(s => s.Token == token);
        if (bgJob == null)
        {
            throw new ArgumentException("Cannot find background job to delete");
        }

        assetContext.Remove(bgJob);
        await assetContext.SaveChangesAsync();

        return bgJob.JobId;
    }

    public static async Task<string> GetTransferTypeNameAsync(this AssetContext assetContext, int transferTypeId)
    {
        TransferType? transferType = await assetContext.TransferTypes.FindAsync(transferTypeId);
        if (transferType == null)
        {
            throw new ArgumentException("Transfer Type cannot be found.");
        }
        return transferType.Name;
    }

    public static async Task DeleteTokensAsync(this AssetContext assetContext, Guid token)
    {
        var identifiedTokens = assetContext.ConfirmationTokens.Where(ct => ct.Token == token);
        var unidentifiedTokens = assetContext.ConfirmationTokensUd.Where(ctu => ctu.Token == token);

        assetContext.ConfirmationTokens.RemoveRange(identifiedTokens);
        assetContext.ConfirmationTokensUd.RemoveRange(unidentifiedTokens);
        await assetContext.SaveChangesAsync();
    }

}