using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Logging;

namespace DealerSetu_Services.Services
{
    public class PerformanceSheetService : IPerformanceSheetService
    {
        private readonly IPerformanceSheetRepo _repository;
        private readonly ILogger<PerformanceSheetService> _logger;

        public PerformanceSheetService(IPerformanceSheetRepo repository, ILogger<PerformanceSheetService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<DealerModel>> GetTrackingDealersAsync(string? keyword, string month, string fYear, string empNo)
        {
            try
            {
                return await _repository.GetTrackingDealersAsync(keyword, month, fYear, empNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackingDealerService.GetTrackingDealersAsync for empNo: {EmpNo}", empNo);
                throw;
            }
        }
    }
}
