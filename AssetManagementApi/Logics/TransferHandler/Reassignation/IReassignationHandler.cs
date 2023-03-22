namespace AssetManagementApi.Logics.TransferHandler.Reassignation;

public interface IReassignationHandler
{

    Task HandleReassignationAsync(int deviceId, string userName, bool sendEmail, Guid? token);
}