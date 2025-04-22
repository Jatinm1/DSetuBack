using DealerSetu_Data;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.IRepositories
{
    public interface ILoginRepository
    {
        Task<dynamic> LoginRepo(LoginModel loginModel); // Fetch user details from SP
        Task<dynamic> LDAPLoginRepo(LoginModel loginModel, bool ldapValidated);
        Task<dynamic> LogoutRepo(string empNo);
        Task<List<PendingCountModel>> PendingCountRepo(string empNo, string roleId);
        Task<bool> UpdateLoginHeartBeatRepo(string empNo);
        Task<bool> UpdateRegularHeartBeatRepo(string empNo);



    }

}
