using AssetManagementApi.Models.DTO;
using AssetManagementApi.Models;

namespace AssetManagementApi.Logics.TokenValidation;

public interface IBulkTokenHandler
{

    Task<Guid> AddNewTokenToDbAsync(List<CartDTO> carts);

    Task SendEmailConfirmationAsync(List<CartDTO> carts, Guid batchId, string domainName);

    Task<bool> ValidateTokenAsync(Guid token, bool userConfirmed, string? declinedReason);

    Task HandleExpiredTokenAsync(List<CartDTO> carts, Guid batchId);

}
