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
    public interface IWhiteVillageService
    {
        Task<IEnumerable<WhiteVillageModel>> GetWhiteListingService();
        Task<ServiceResponse> UploadWhiteVillageFileService(IFormFile excelFile, string stateId, string empNo);
        Task<ServiceResponse> GetStateListService();
        Task<string> WhiteVillageDownloadService(string fileName);
        Task<List<string>> ListAllBlobsInContainer();
        Task<bool> DeleteBlobFile(string fileName);



    }
}
