using AssetManagementApi.Models;

namespace AssetManagementApi.Logics.TransferHandler.Bulk;

public interface IBulkHandler
{

    Task HandleMultipleTransfersForBothCategories(List<ConfirmationToken> identified, List<ConfirmationTokenUd> unidentified, string userName);
}