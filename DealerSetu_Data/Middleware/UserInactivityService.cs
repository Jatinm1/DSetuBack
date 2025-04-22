using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

public class UserInactivityService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly string _connString;
    private readonly int _checkIntervalSeconds;
    private readonly int _inactivityTimeoutSeconds;

    public UserInactivityService(IServiceProvider services, IConfiguration configuration)
    {
        _services = services;
        _connString = configuration.GetConnectionString("dbDealerSetuEntities")
                      ?? throw new ArgumentNullException(nameof(_connString), "Database connection string is missing.");

        // Fetch values from appsettings.json
        _checkIntervalSeconds = configuration.GetValue<int>("UserInactivity:CheckIntervalSeconds", 300);
        _inactivityTimeoutSeconds = configuration.GetValue<int>("UserInactivity:InactivityTimeoutSeconds", 120);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await LogoutInactiveUsers();
                await Task.Delay(TimeSpan.FromSeconds(_checkIntervalSeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                // Log the error properly (if you have a logging service, use that)
                Console.WriteLine($"[Error] {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Back off on error
            }
        }
    }

    private async Task LogoutInactiveUsers()
    {
        await using var connection = new SqlConnection(_connString);
        await connection.OpenAsync();

        var parameters = new DynamicParameters();
        parameters.Add("@TimeoutSeconds", _inactivityTimeoutSeconds);

        await connection.ExecuteAsync(
            "sp_SESSION_LogoutInactiveUsers",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure
        );
    }
}