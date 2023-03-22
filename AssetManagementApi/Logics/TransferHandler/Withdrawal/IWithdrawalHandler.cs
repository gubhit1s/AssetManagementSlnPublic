namespace AssetManagementApi.Logics.TransferHandler.Withdrawal;

public interface IWithdrawalHandler
{
    Task HandleWithdrawalAsync(int deviceId, string userName, Guid? token, bool sendEmail);
}

