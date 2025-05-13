using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

public class StorageDiagnosticsService
{
    private readonly string _storageConnectionString;
    private readonly string _containerName;
    private readonly ILogger<StorageDiagnosticsService> _logger;

    public StorageDiagnosticsService(
        IConfiguration configuration,
        ILogger<StorageDiagnosticsService> logger)
    {
        _storageConnectionString = configuration.GetValue<string>("AzureBlobStorage:StorageAccount");
        _containerName = configuration.GetValue<string>("AzureBlobStorage:ContainerName");
        _logger = logger;
    }

    public async Task<StorageDiagnosticResult> RunComprehensiveDiagnostics()
    {
        var result = new StorageDiagnosticResult();

        try
        {
            // 1. Connection String Validation
            var connectionStringCheck = ValidateConnectionString();
            result.ConnectionStringValidation = connectionStringCheck.Success;
            if (!connectionStringCheck.Success)
            {
                result.ErrorMessages.Add(connectionStringCheck.ErrorMessage);
                return result;
            }


            // 2. Parse Storage Account
            var storageAccount = ParseStorageAccount();
            result.StorageAccountParsing = storageAccount.Success;
            if (!storageAccount.Success)
            {
                result.ErrorMessages.Add(storageAccount.ErrorMessage);
                return result;
            }

            // 3. Create Blob Client
            var blobClient = CreateBlobClient(storageAccount.StorageAccount);
            result.BlobClientCreation = blobClient.Success;
            if (!blobClient.Success)
            {
                result.ErrorMessages.Add(blobClient.ErrorMessage);
                return result;
            }

            // 4. Check Service Properties
            var servicePropertiesCheck = await CheckServiceProperties(blobClient.BlobClient);
            result.ServicePropertiesCheck = servicePropertiesCheck.Success;
            if (!servicePropertiesCheck.Success)
            {
                result.ErrorMessages.Add(servicePropertiesCheck.ErrorMessage);
            }

            // 5. Container Operations
            var containerCheck = await CheckContainerOperations(blobClient.BlobClient);
            result.ContainerOperations = containerCheck.Success;
            if (!containerCheck.Success)
            {
                result.ErrorMessages.Add(containerCheck.ErrorMessage);
            }

            // 6. Blob Operations Test
            var blobOperationsCheck = await TestBlobOperations(blobClient.BlobClient);
            result.BlobOperations = blobOperationsCheck.Success;
            if (!blobOperationsCheck.Success)
            {
                result.ErrorMessages.Add(blobOperationsCheck.ErrorMessage);
            }

            // Determine overall success
            result.OverallSuccess = result.ConnectionStringValidation
                && result.StorageAccountParsing
                && result.BlobClientCreation
                && result.ServicePropertiesCheck
                && result.ContainerOperations
                && result.BlobOperations;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comprehensive storage diagnostics failed");
            result.OverallSuccess = false;
            result.ErrorMessages.Add($"Unexpected error: {ex.Message}");
            return result;
        }
    }

    private DiagnosticCheckResult ValidateConnectionString()
    {
        try
        {
            if (string.IsNullOrEmpty(_storageConnectionString))
                return new DiagnosticCheckResult(false, "Connection string is null or empty");

            bool hasProtocol = _storageConnectionString.Contains("DefaultEndpointsProtocol=");
            bool hasAccountName = _storageConnectionString.Contains("AccountName=");
            bool hasAccountKey = _storageConnectionString.Contains("AccountKey=");
            bool hasEndpoint = _storageConnectionString.Contains("EndpointSuffix=");

            bool isValid = hasProtocol && hasAccountName && hasAccountKey && hasEndpoint;

            return new DiagnosticCheckResult(isValid,
                isValid ? "Connection string is valid" : "Connection string is missing required components");
        }
        catch (Exception ex)
        {
            return new DiagnosticCheckResult(false, $"Connection string validation error: {ex.Message}");
        }
    }

    private DiagnosticCheckResult ParseStorageAccount()
    {
        try
        {
            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            return new DiagnosticCheckResult(true, "Storage account parsed successfully")
            {
                StorageAccount = storageAccount
            };
        }
        catch (Exception ex)
        {
            return new DiagnosticCheckResult(false, $"Storage account parsing failed: {ex.Message}");
        }
    }

    private DiagnosticCheckResult CreateBlobClient(CloudStorageAccount storageAccount)
    {
        try
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            return new DiagnosticCheckResult(true, "Blob client created successfully")
            {
                BlobClient = blobClient
            };
        }
        catch (Exception ex)
        {
            return new DiagnosticCheckResult(false, $"Blob client creation failed: {ex.Message}");
        }
    }

    private async Task<DiagnosticCheckResult> CheckServiceProperties(CloudBlobClient blobClient)
    {
        try
        {
            var serviceProperties = await blobClient.GetServicePropertiesAsync();
            return new DiagnosticCheckResult(true, "Service properties retrieved successfully");
        }
        catch (Exception ex)
        {
            return new DiagnosticCheckResult(false, $"Service properties check failed: {ex.Message}");
        }
    }

    private async Task<DiagnosticCheckResult> CheckContainerOperations(CloudBlobClient blobClient)
    {
        try
        {
            var container = blobClient.GetContainerReference(_containerName);

            // Check container existence
            bool exists = await container.ExistsAsync();
            _logger.LogInformation("Container {ContainerName} exists: {Exists}", _containerName, exists);

            // Try to get container permissions (helps diagnose access issues)
            try
            {
                var permissions = await container.GetPermissionsAsync();
                _logger.LogInformation("Container access type: {AccessType}", permissions.PublicAccess);
            }
            catch (Exception permEx)
            {
                _logger.LogWarning("Unable to read container permissions: {ErrorMessage}", permEx.Message);
            }

            // If not exists, try to create
            if (!exists)
            {
                await container.CreateIfNotExistsAsync(
                    BlobContainerPublicAccessType.Off,
                    new BlobRequestOptions(),
                    new OperationContext()
                );
            }

            // List containers to check list permissions
            var containers = await ListContainers(blobClient);

            return new DiagnosticCheckResult(true,
                $"Container operations successful. Container exists: {exists}. " +
                $"Total containers found: {containers.Count}");
        }
        catch (Exception ex)
        {
            return new DiagnosticCheckResult(false, $"Container operations failed: {ex.Message}");
        }
    }

    private async Task<List<CloudBlobContainer>> ListContainers(CloudBlobClient blobClient)
    {
        BlobContinuationToken continuationToken = null;
        List<CloudBlobContainer> containers = new List<CloudBlobContainer>();

        do
        {
            var results = await blobClient.ListContainersSegmentedAsync(continuationToken);
            continuationToken = results.ContinuationToken;
            containers.AddRange(results.Results);
        }
        while (continuationToken != null);

        return containers;
    }

    private async Task<DiagnosticCheckResult> TestBlobOperations(CloudBlobClient blobClient)
    {
        try
        {
            var container = blobClient.GetContainerReference(_containerName);

            // Create a test blob
            var testBlob = container.GetBlockBlobReference($"test-blob-{Guid.NewGuid()}.txt");

            // Upload test content
            await testBlob.UploadTextAsync("Storage diagnostics test content");

            // Download to verify
            string downloadedContent = await testBlob.DownloadTextAsync();

            // Delete test blob
            await testBlob.DeleteAsync();

            return new DiagnosticCheckResult(true, "Blob operations test successful");
        }
        catch (Exception ex)
        {
            return new DiagnosticCheckResult(false, $"Blob operations test failed: {ex.Message}");
        }
    }
}

// Supporting Classes for Diagnostic Results
public class StorageDiagnosticResult
{
    public bool OverallSuccess { get; set; }
    public bool ConnectionStringValidation { get; set; }
    public bool StorageAccountParsing { get; set; }
    public bool BlobClientCreation { get; set; }
    public bool ServicePropertiesCheck { get; set; }
    public bool ContainerOperations { get; set; }
    public bool BlobOperations { get; set; }
    public List<string> ErrorMessages { get; set; } = new List<string>();
}

public class DiagnosticCheckResult
{
    public bool Success { get; }
    public string ErrorMessage { get; }
    public CloudStorageAccount StorageAccount { get; set; }
    public CloudBlobClient BlobClient { get; set; }

    public DiagnosticCheckResult(bool success, string errorMessage = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }
}