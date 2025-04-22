using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface IMasterService
    {
        Task<ServiceResponse> EmployeeMasterService(string keyword, string role, int pageIndex, int pageSize);
        Task<UpdateResult> UpdateEmployeeDetailsService(int? userId, string empName, string email);
        Task<ServiceResponse> ProcessEmployeeExcelFile(IFormFile file, string role);
        Task<EncryptedBlobData> UploadEncryptedExcelToBlob(IFormFile file);
        bool ValidateExcelFile(IFormFile file, out string errorMessage);
        Task<List<EmployeeModel>> ProcessEncryptedEmployeeExcelFromBlob(EncryptedBlobData blobData);
        Task<ServiceResponse> ProcessDealerExcelFile(IFormFile file);
        Task<string> GenerateDownloadUrlAsync(string fileName);
        Task<string> DeleteUserService(int userId);
        Task<ServiceResponse> DealerMasterService(string keyword, int pageIndex, int pageSize);
        Task<ServiceResponse> RolesDropdownService();
        Task<UpdateResult> UpdateDealerDetailsService(int? userId, string empName, string location, string district, string zone, string state);
    }
}
