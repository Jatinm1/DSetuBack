using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.IRepositories
{
    public interface IWhiteVillageRepository
    {
        Task<IEnumerable<WhiteVillageModel>> GetWhiteListingRepo();
        Task<string> SaveWhiteVillageFileMetadata(string blobUrl, string stateId, string createdBy, string fiscalYear);
        Task<List<StateModel>> GetStateListRepo();
        Task<string> WhiteVillageDownloadRepo(string fileName);



    }
}
