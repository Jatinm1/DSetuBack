using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu_Services.IServices
{
    public interface IPerformanceSheetService
    {
        Task<IEnumerable<DealerModel>> GetTrackingDealersAsync(string? keyword, int month, string fYear, string empNo);

    }
}
