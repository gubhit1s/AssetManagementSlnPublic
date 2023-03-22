using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Logics.TransferHandler.Assignation;
using AssetManagementApi.Logics.TransferHandler.Reassignation;
using AssetManagementApi.Logics.TransferHandler.Repair;
using AssetManagementApi.Logics.TransferHandler.Unidentified;
using AssetManagementApi.Logics.TransferHandler.Withdrawal;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Helpers;
using System.Text;

namespace AssetManagementApi.Logics.TransferHandler.Bulk;

public class BulkHandler : BaseTransferLogic, IBulkHandler
{

    private readonly ILogger<BulkHandler> _logger;
    private readonly IAssignationHandler _assignationHandler;
    private readonly IReassignationHandler _reassignationHandler;
    private readonly RepairHandler _repairHandler;
    private readonly UnidentifiedTransferHandler _unidentifiedHandler;
    private readonly IWithdrawalHandler _withdrawalHandler;
    private readonly BulkTransferHelper _bulkHelper;

    public BulkHandler(
        AssetContext assetContext,
        IEmailHandler emailHandler,
        IAdUser adUser,
        IAssignationHandler assignationHandler,
        IReassignationHandler reassignationHandler,
        RepairHandler repairHandler,
        IWithdrawalHandler withdrawalHandler,
        UnidentifiedTransferHandler unidentifiedHandler,
        ILogger<BulkHandler> logger,
        BulkTransferHelper bulkHelper
    ) : base(assetContext, emailHandler, adUser)
    {
        _assignationHandler = assignationHandler;
        _reassignationHandler = reassignationHandler;
        _repairHandler = repairHandler;
        _unidentifiedHandler = unidentifiedHandler;
        _withdrawalHandler = withdrawalHandler;
        _logger = logger;
        _bulkHelper = bulkHelper;
    }

    public async Task HandleMultipleTransfersForBothCategories(List<ConfirmationToken> identified, List<ConfirmationTokenUd> unidentified, string userName)
    {
        _logger.LogInformation("Bulk background job started");
        await HandleMultipleTransfersForIdentifiedAsync(identified);
        await HandleMultipleTransfersForUnidentifiedAsync(unidentified);

        string displayName = _adUser.GetDisplayName(userName);
        string email = _adUser.GetEmail(userName);

        List<CartDTO> carts = await _bulkHelper.TokensToCartDTOAsync(identified, unidentified);
        string deviceDetails = await _bulkHelper.GenerateCartDetailsAsync(carts);

        await SendSuccessNotificationEmailAsync("Bulk Transfer: User has confirmed",
            $"<p>User <b>{displayName}</b>({email}) has confirmed the transfer of <b>{identified.Count + unidentified.Count}</b> devices. </p>" +
            $"{deviceDetails}<p> The inventory has successfully been updated.</p>");
        _logger.LogInformation("Bulk background job finished");
    }

    public async Task HandleMultipleTransfersForIdentifiedAsync(List<ConfirmationToken> identifiedTokens)
    {
        foreach (ConfirmationToken token in identifiedTokens)
        {
            int transfer = token.TransferTypeId;
            switch (transfer)
            {
                case (TransferTypes.Assignation):
                    await _assignationHandler.HandleAssignationAsync(token.DeviceId, token.UserName!, null, false);
                    break;
                case (TransferTypes.Reassignation):
                    await _reassignationHandler.HandleReassignationAsync(token.DeviceId, token.UserName!, false, null);
                    break;
                case (TransferTypes.MaintenanceRepairing):
                    await _repairHandler.HandleRepairFromUser(token.DeviceId, token.UserName!, false);
                    break;
                case (TransferTypes.Withdrawal):
                    await _withdrawalHandler.HandleWithdrawalAsync(token.DeviceId, token.UserName!, null, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unable to find proper transfer type to process.");
            }
        }
        
    }

    public async Task HandleMultipleTransfersForUnidentifiedAsync(List<ConfirmationTokenUd> unidentifiedTokens)
    {
        foreach (ConfirmationTokenUd token in unidentifiedTokens)
        {
            await _unidentifiedHandler.HandleTransferUdDevicesInvolvingUserAsync(token, false);
        }
    }
    

}







