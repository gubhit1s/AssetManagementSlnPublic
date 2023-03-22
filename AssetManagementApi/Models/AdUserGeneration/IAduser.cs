namespace AssetManagementApi.Models.AdUserGeneration;

public interface IAdUser
{
    IEnumerable<User> Users { get; }
    IEnumerable<User> GetAllActiveAdUsers();

    string GetEmail(string userName);

    string GetDisplayName(string userName);
}