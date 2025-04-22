using DealerSetu_Data;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface ILoginService
    {
        Task<ServiceResponse> Login_Service(LoginModel loginModel);
        Task<ServiceResponse> LDAPLoginService(LoginModel loginModel);
        Task<ServiceResponse> LogOutService(string empNo);
        Task<ServiceResponse> PendingCountService(FilterModel filter);
        Task<bool> UpdateLoginHeartbeatService(string empNo);
        Task<bool> UpdateRegularHeartbeatService(string empNo);

    }
}
