using AssetManagementApi.Models;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Diagnostics;

namespace AssetManagementApi.Models.AdUserGeneration;

#pragma warning disable CA1416 // Validate platform compatibility
public class AdUser : IAdUser
{
    private static readonly IConfiguration _configuration;


    public IEnumerable<User> Users
    {
        get
        {
            return GetAllActiveAdUsers();
        }
    }

    public IEnumerable<User> GetAllActiveAdUsers()
    {
        IEnumerable<User> hnUsers = GetActiveUsersByOffice("Hanoi");
        IEnumerable<User> hcmUsers = GetActiveUsersByOffice("Ho Chi Minh");
        return hnUsers.Concat(hcmUsers);
    }


    /// <summary>
    /// Get all active users within Vietnam region, by office
    /// </summary>
    /// <param name="office"></param>
    /// <returns></returns>
    public static IEnumerable<User> GetActiveUsersByOffice(string office)
    {
        string[] properties = new[] { "displayName", "samAccountName", "department", "userPrincipalName" };
        using (var context = new PrincipalContext(ContextType.Domain, "tmf-group.com", _configuration["ActiveDirectoryOfficePath"]))
        {
            using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
            {
                //Casting PrincipalSearchResult to DirectoryEntry is ridiculously slow, so we use DirectorySearcher instead.

                List<User> result = new();
                DirectorySearcher? ds = searcher.GetUnderlyingSearcher() as DirectorySearcher;
                ds!.PropertiesToLoad.Clear();
                ds!.PropertiesToLoad.AddRange(properties);

                foreach (SearchResult searchResult in ds.FindAll())
                {

                    User user = new User()
                    {
                        UserName = searchResult.Properties["samAccountName"][0].ToString(),
                        DisplayName = searchResult.Properties["displayName"][0].ToString(),
                        Email = searchResult.Properties["userPrincipalName"][0].ToString(),
                        Department = searchResult.Properties["department"][0].ToString(),
                        Office = office
                    };

                    result.Add(user);

                }
                return result;
            }
        }
    }

    public string GetEmail(string userName)
    {
        User? user = Users.FirstOrDefault(u => u.UserName!.ToLower() == userName.ToLower());
        if (user is not null)
        {
            return user.Email!;
        }
        return "";
    }

    public string GetDisplayName(string userName)
    {
        User? user = Users.FirstOrDefault(u => u.UserName!.ToLower() == userName.ToLower());
        if (user is not null)
        {
            return user.DisplayName!;
        }
        return "";
    }

}
#pragma warning restore CA1416 // Validate platform compatibility