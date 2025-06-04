using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DealerSetu_Repositories.Repositories
{
    public class PerformanceSheetRepo : IPerformanceSheetRepo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PerformanceSheetRepo> _logger;
        private readonly string _connectionString;

        public PerformanceSheetRepo(IConfiguration configuration, ILogger<PerformanceSheetRepo> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<DealerModel>> GetTrackingDealersAsync(string? keyword, string month, string fYear, string empNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@Keyword", keyword, DbType.String);
                parameters.Add("@Month", month, DbType.String);
                parameters.Add("@FYear", fYear, DbType.String);
                parameters.Add("@EmpNo", empNo, DbType.String);

                var dealers = await connection.QueryAsync<DealerModel>(
                    "sp_PERF_GetTrackingDealers",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                return dealers ?? Enumerable.Empty<DealerModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tracking dealers for empNo: {EmpNo}, month: {Month}, year: {FYear}",
                    empNo, month, fYear);
                throw;
            }
        }
    }
}
