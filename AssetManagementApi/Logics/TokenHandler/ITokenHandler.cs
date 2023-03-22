using AssetManagementApi.Models.DTO;

namespace AssetManagementApi.Logics.TokenValidation;

public interface ITokenHandler
{
    /// <summary>
    /// Capture device and user information and generate a token with a fixed expiry date, then add all of these into the db.
    /// </summary>
    /// <param name="deviceCode">the id of the device, or device type</param>
    /// <param name="userName">the user name that is used to login as an alternative of user's email.</param>
    /// <param name="transferTypeId">The transfer type.</param>
    /// <returns>the token as a GUID</returns>
    Task<Guid> AddNewTokenToDbAsync(int deviceCode, string userName, int transferTypeId);

    /// <summary>
    ///Search for the given token in the 'ConfirmationTokens' or 'ConfirmationTokensUd' table and check if it exists and does not expire yet.
    /// If the token does exists, call a background work based on the transfer type.
    /// </summary>
    /// <param name="token">The give token</param>
    /// <param name="declinedReason">the reason, if exists, why user declines to confirm.</param>
    /// <returns></returns>
    Task<bool> ValidateTokenAsync(Guid token, bool userConfirmed, string? declinedReason);

    /// <summary>
    /// Send the confirmation email to user, with details of the assigning device and a url with token.
    /// </summary>
    /// <param name="deviceId">The id of the device, or device type</param>
    /// <param name="userName">The user name</param>
    /// <param name="token">The token, it must match with the one generated in the db.</param>
    /// <returns></returns>
    Task SendEmailConfirmationAsync(int deviceCode, string userName, Guid token, int transferTypeId, string domainName);

    Task HandleExpiredTokenAsync(ConfirmationTokenDTO confirmationTokens);

    static int expirationHour = 24;
}