using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu_Repositories.IRepositories
{
    public interface IMasterRepository
    {
        Task<EmployeeListResult> EmployeeMasterRepo(string keyword, string role, int pageIndex, int pageSize);
        Task<List<RoleModel>> RolesDropdownRepo();
        Task<AdditionResponse<string>> AddEmployeeRepo(EmployeeModel emp);
        Task<AdditionResponse<string>> AddDealerRepo(DealerModel dealer);
        Task<string> DeleteUserRepo(int userId);
        Task<DealerListResult> DealerMasterRepo(string keyword, int pageIndex, int pageSize);
        Task<string> GetBlobUrlByFileNameAsync(string fileName);
        Task<(int RowsAffected, string Message)> UpdateEmployeeDetailsRepo(int? userId, string? name, string? email);
        Task<(int RowsAffected, string Message)> UpdateDealerDetailsRepo(int? userId, string? empName, string? location, string? district, string? zone, string? state);    }
}
