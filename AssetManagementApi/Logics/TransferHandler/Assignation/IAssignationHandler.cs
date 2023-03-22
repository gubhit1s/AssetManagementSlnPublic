namespace AssetManagementApi.Logics.TransferHandler.Assignation;

public interface IAssignationHandler
{

    Task HandleAssignationAsync(int deviceId, string userName, Guid? token, bool sendEmail);
}
