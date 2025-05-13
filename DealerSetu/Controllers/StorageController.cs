using DealerSetu_Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    private readonly StorageDiagnosticsService _storageDiagnosticsService;
    private readonly ILogger<StorageController> _logger;
    private readonly JwtHelper _jwtHelper;
    private readonly IConfiguration _configuration;



    public StorageController(
        StorageDiagnosticsService storageDiagnosticsService,
        ILogger<StorageController> logger, JwtHelper jwtHelper, IConfiguration configuration)
    {
        _storageDiagnosticsService = storageDiagnosticsService;
        _logger = logger;
        _jwtHelper = jwtHelper;
        _configuration = configuration;
    }

    [HttpGet("diagnose")]
    public async Task<IActionResult> DiagnoseStorage()
    {
        try
        {
            var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
            var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");
            var diagnosticResult = await _storageDiagnosticsService.RunComprehensiveDiagnostics();

            // Log the results
            _logger.LogInformation("Storage Diagnostics Completed. Success: {Success}",
                diagnosticResult.OverallSuccess);

            if (!diagnosticResult.OverallSuccess)
            {
                _logger.LogWarning("Storage Diagnostics Errors: {Errors}",
                    string.Join(", ", diagnosticResult.ErrorMessages));
            }

            return Ok(new
            {
                success = diagnosticResult.OverallSuccess,
                details = new
                {
                    connectionStringValidation = diagnosticResult.ConnectionStringValidation,
                    storageAccountParsing = diagnosticResult.StorageAccountParsing,
                    blobClientCreation = diagnosticResult.BlobClientCreation,
                    servicePropertiesCheck = diagnosticResult.ServicePropertiesCheck,
                    containerOperations = diagnosticResult.ContainerOperations,
                    blobOperations = diagnosticResult.BlobOperations,
                    errorMessages = diagnosticResult.ErrorMessages
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during storage diagnostics");
            return StatusCode(500, new
            {
                success = false,
                error = "An unexpected error occurred during storage diagnostics"
            });
        }
    }

    [HttpGet("test-access")]
    public async Task<IActionResult> TestAccess()
    {
        try
        {
            // Parse connection string
            var storageAccount = CloudStorageAccount.Parse(
                _configuration.GetValue<string>("AzureBlobStorage:StorageAccount"));

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(
                _configuration.GetValue<string>("AzureBlobStorage:ContainerName"));

            // List blobs and log their names
            var blobs = new List<string>();
            BlobContinuationToken token = null;

            do
            {
                var segment = await container.ListBlobsSegmentedAsync(token);
                token = segment.ContinuationToken;

                foreach (var blob in segment.Results)
                {
                    blobs.Add(blob.Uri.ToString());
                }
            } while (token != null);

            return Ok(new
            {
                success = true,
                message = "Successfully accessed storage",
                blobCount = blobs.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}