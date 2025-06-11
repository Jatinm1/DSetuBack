using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Logging;

namespace DealerSetu_Services.Services
{
    public class PerformanceSheetService : IPerformanceSheetService
    {
        private readonly IPerformanceSheetRepo _perfRepository;
        private readonly ILogger<PerformanceSheetService> _logger;

        public PerformanceSheetService(IPerformanceSheetRepo perfRepository, ILogger<PerformanceSheetService> logger)
        {
            _perfRepository = perfRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<DealerModel>> GetTrackingDealersAsync(string? keyword, int month, string fYear, string empNo)
        {
            try
            {
                return await _perfRepository.GetTrackingDealersAsync(keyword, month, fYear, empNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackingDealerService.GetTrackingDealersAsync for empNo: {EmpNo}", empNo);
                throw;
            }
        }

        public async Task<PerformanceSheetModel> GetPerformanceSheetServiceAsync(PerformanceSheetReqModel request)
        {
            try
            {
                if (request.DealerEmpId <= 0)
                    throw new ArgumentException("DealerEmpId must be greater than 0", nameof(request.DealerEmpId));

                if (request.Month < 1 || request.Month > 12)
                    throw new ArgumentException("Month must be between 1 and 12", nameof(request.Month));
                

                if (request.FYear == null || request.FYear == "")
                {
                    throw new ArgumentException("FYear cannot be null or empty", nameof(request.FYear));
                }

                return await _perfRepository.GetPerformanceSheetRepoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPerformanceSheetAsync for DealerEmpId: {dealerEmpId}", request.DealerEmpId);
                throw;
            }
        }
    }
}
