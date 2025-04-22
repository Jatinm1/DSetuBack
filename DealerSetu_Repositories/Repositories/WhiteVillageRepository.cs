using Dapper;
using DealerSetu_Data.Models.ViewModels;
using dealersetu_repositories.irepositories;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.Repositories
{
    public class WhiteVillageRepository : IWhiteVillageRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<WhiteVillageRepository> _logger;

        public WhiteVillageRepository(IConfiguration configuration, ILogger<WhiteVillageRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities");
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the white listing data from the database.
        /// </summary>
        public async Task<IEnumerable<WhiteVillageModel>> GetWhiteListingRepo()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                return await connection.QueryAsync<WhiteVillageModel>("sp_WHITEVILLAGE_WhiteVillageList", commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetWhiteListing stored procedure");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the list of states from the database.
        /// </summary>
        public async Task<List<StateModel>> GetStateListRepo()
        {
            using var connection = new SqlConnection(_connectionString);
            try
            {
                return connection.Query<StateModel>("sp_WHITEVILLAGE_GetStateList", commandType: CommandType.StoredProcedure).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving state list");
                throw;
            }
        }

        /// <summary>
        /// Saves metadata for the uploaded White Village file in the database.
        /// </summary>
        public async Task<string> SaveWhiteVillageFileMetadata(string filename, string stateId, string createdBy, string fiscalYear)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@StateId", stateId);
            parameters.Add("@Filename", filename); // Blob URL
            parameters.Add("@CreatedBy", createdBy);
            parameters.Add("@CreatedDate", DateTime.Now);
            parameters.Add("@FYear", fiscalYear);
            parameters.Add("@Result", dbType: DbType.String, direction: ParameterDirection.Output, size: 10);

            await connection.ExecuteAsync("sp_WHITEVILLAGE_SaveExcelFileMetadata", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<string>("@Result");
        }

        /// <summary>
        /// Retrieves the Blob URL for downloading a White Village file.
        /// </summary>
        public async Task<string> WhiteVillageDownloadRepo(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Filename cannot be null or empty.", nameof(fileName));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<string>(
                "sp_WHITEVILLAGE_FileDownload",
                new { FileName = fileName },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}