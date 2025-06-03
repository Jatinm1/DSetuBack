using DealerSetu.Repository.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class MasterService : IMasterService
    {
        private readonly IMasterRepository _masterRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IFileValidationService _fileValidationService;
        private readonly IConfiguration _configuration;
        private readonly Utility _utility;
        private readonly string _storageConnectionString;
        private readonly string _containerName;
        private const int MaxFileSize = 4 * 1024 * 1024; // 4MB

        #region VALIDATION FLAGS - Enable/Disable validations by commenting/uncommenting

        // File Upload Validations
        private const bool ENABLE_FILE_NULL_CHECK = true;
        private const bool ENABLE_FILE_SIZE_VALIDATION = true;
        private const bool ENABLE_FILE_EXTENSION_VALIDATION = true;
        private const bool ENABLE_FILE_SIGNATURE_VALIDATION = true;
        private const bool ENABLE_FILE_CONTENT_VALIDATION = true;

        // Data Input Validations
        private const bool ENABLE_ROLE_ID_VALIDATION = true;
        private const bool ENABLE_USER_ID_VALIDATION = true;
        private const bool ENABLE_EMAIL_FORMAT_VALIDATION = true;
        private const bool ENABLE_MALICIOUS_CONTENT_CHECK = true;
        private const bool ENABLE_ALPHANUMERIC_VALIDATION = true;

        // Business Logic Validations
        private const bool ENABLE_EMPTY_DATA_CHECK = true;
        private const bool ENABLE_DUPLICATE_CHECK = false; // Can be enabled later if needed

        #endregion

        public MasterService(
            IMasterRepository masterRepository,
            IConfiguration configuration,
            Utility utility,
            IEncryptionService encryptionService,
            IFileValidationService fileValidationService)
        {
            _masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _fileValidationService = fileValidationService ?? throw new ArgumentNullException(nameof(fileValidationService));

            _storageConnectionString = _configuration.GetValue<string>("AzureBlobStorage:StorageAccount");
            _containerName = _configuration.GetValue<string>("AzureBlobStorage:ContainerName");

            if (string.IsNullOrEmpty(_storageConnectionString))
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

            if (string.IsNullOrEmpty(_containerName))
                throw new InvalidOperationException("Azure Blob Storage container name is not configured.");

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        #region MAIN SERVICE METHODS

        public async Task<ServiceResponse> EmployeeMasterService(string keyword, string role, int pageIndex, int pageSize)
        {
            try
            {
                var result = await _masterRepository.EmployeeMasterRepo(keyword, role, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = result.Employees,
                    totalCount = result.TotalCount,
                    Message = "Employees retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error retrieving employees");
            }
        }

        public async Task<ServiceResponse> DealerMasterService(string keyword, int pageIndex, int pageSize)
        {
            try
            {
                var result = await _masterRepository.DealerMasterRepo(keyword, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = result.Dealers,
                    totalCount = result.TotalCount,
                    Message = "Dealers retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error retrieving dealers");
            }
        }

        public async Task<ServiceResponse> RolesDropdownService()
        {
            try
            {
                var roles = await _masterRepository.RolesDropdownRepo();

                return new ServiceResponse
                {
                    isError = false,
                    result = roles,
                    Message = "Roles retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error retrieving roles");
            }
        }

        public async Task<ServiceResponse> ProcessEmployeeExcelFile(IFormFile file, string role)
        {
            // VALIDATION BLOCK 1: File Upload Validations
            var fileValidationResult = ValidateFileUpload(file);
            if (fileValidationResult != null)
                return fileValidationResult;

            // VALIDATION BLOCK 2: Role ID Validation
            var roleValidationResult = ValidateRoleId(role);
            if (roleValidationResult != null)
                return roleValidationResult;

            // VALIDATION BLOCK 3: File Content Validations
            var contentValidationResult = await ValidateFileContent(file);
            if (contentValidationResult != null)
                return contentValidationResult;

            // VALIDATION BLOCK 4: Excel Format Validation
            var excelValidationResult = ValidateExcelFormat(file);
            if (excelValidationResult != null)
                return excelValidationResult;

            EncryptedBlobData blobData = null;

            try
            {
                // Upload and encrypt the file
                blobData = await UploadEncryptedExcelToBlob(file);

                // Process the file and save employees
                var employees = await ProcessEncryptedEmployeeExcelFromBlob(blobData);

                // VALIDATION BLOCK 5: Empty Data Check
                var dataValidationResult = ValidateProcessedData(employees?.Count ?? 0, "employee");
                if (dataValidationResult != null)
                    return dataValidationResult;

                // Process each employee record
                int.TryParse(role, out int roleId);
                foreach (var employee in employees)
                {
                    employee.RoleId = roleId;
                    employee.Password = employee.EmpNo?.Trim();

                    var saveResponse = await _masterRepository.AddEmployeeRepo(employee);
                    if (saveResponse.IsError)
                        return CreateResponse(true, $"Error saving employee {employee.EmpNo}: {saveResponse.Message}", "500");
                }

                return CreateResponse(false, "Employees added successfully.", "200");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "An error occurred while processing the employee file");
            }
            finally
            {
                // Cleanup: delete the blob file whether processing succeeded or failed
                if (blobData != null)
                {
                    try
                    {
                        await DeleteBlobFile(blobData.BlobUrl);
                    }
                    catch
                    {
                        // Suppress blob deletion errors - consider logging instead
                    }
                }
            }
        }

        public async Task<ServiceResponse> ProcessDealerExcelFile(IFormFile file)
        {
            // VALIDATION BLOCK 1: File Upload Validations
            var fileValidationResult = ValidateFileUpload(file);
            if (fileValidationResult != null)
                return fileValidationResult;

            // VALIDATION BLOCK 3: File Content Validations
            var contentValidationResult = await ValidateFileContent(file, isDealerFile: true);
            if (contentValidationResult != null)
                return contentValidationResult;

            // VALIDATION BLOCK 4: Excel Format Validation
            var excelValidationResult = ValidateExcelFormat(file);
            if (excelValidationResult != null)
                return excelValidationResult;

            EncryptedBlobData blobData = null;

            try
            {
                // Upload and encrypt the file
                blobData = await UploadEncryptedExcelToBlob(file);

                // Process the file and save dealers
                var dealers = await ProcessEncryptedDealerExcelFromBlob(blobData);

                // VALIDATION BLOCK 5: Empty Data Check
                var dataValidationResult = ValidateProcessedData(dealers?.Count ?? 0, "dealer");
                if (dataValidationResult != null)
                    return dataValidationResult;

                // Process each dealer record
                foreach (var dealer in dealers)
                {
                    var saveResponse = await _masterRepository.AddDealerRepo(dealer);
                    if (saveResponse.IsError)
                        return CreateResponse(true, $"Error saving dealer {dealer.DealerNo}: {saveResponse.Message}", "500");
                }

                return CreateResponse(false, "Dealers added successfully.", "200");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "An error occurred while processing the dealer file");
            }
            finally
            {
                // Cleanup: delete the blob file whether processing succeeded or failed
                if (blobData != null)
                {
                    try
                    {
                        await DeleteBlobFile(blobData.BlobUrl);
                    }
                    catch
                    {
                        // Suppress blob deletion errors - consider logging instead
                    }
                }
            }
        }

        public async Task<UpdateResult> UpdateEmployeeDetailsService(int? userId, string empName, string email)
        {
            try
            {
                // VALIDATION BLOCK 6: User ID Validation
                var userIdValidationResult = ValidateUserId(userId);
                if (userIdValidationResult != null)
                    return userIdValidationResult;

                // VALIDATION BLOCK 7: Employee Name Validation
                var nameValidationResult = ValidateEmployeeName(empName);
                if (nameValidationResult != null)
                    return nameValidationResult;

                // VALIDATION BLOCK 8: Email Validation
                var emailValidationResult = ValidateEmailInput(email);
                if (emailValidationResult != null)
                    return emailValidationResult;

                var result = await _masterRepository.UpdateEmployeeDetailsRepo(userId, empName, email);
                return new UpdateResult
                {
                    RowsAffected = result.RowsAffected,
                    Message = result.Message,
                    IsSuccess = result.RowsAffected > 0
                };
            }
            catch (Exception ex)
            {
                return CreateUpdateErrorResult($"An error occurred: {ex.Message}");
            }
        }

        public async Task<UpdateResult> UpdateDealerDetailsService(int? userId, string empName, string location, string district, string zone, string state)
        {
            try
            {
                // VALIDATION BLOCK 6: User ID Validation
                var userIdValidationResult = ValidateUserId(userId);
                if (userIdValidationResult != null)
                    return userIdValidationResult;

                // VALIDATION BLOCK 9: Dealer Details Validation
                var dealerDetailsValidationResult = ValidateDealerDetails(empName, location, district, zone, state);
                if (dealerDetailsValidationResult != null)
                    return dealerDetailsValidationResult;

                var result = await _masterRepository.UpdateDealerDetailsRepo(userId, empName, location, district, zone, state);
                return new UpdateResult
                {
                    RowsAffected = result.RowsAffected,
                    Message = result.Message,
                    IsSuccess = result.RowsAffected > 0
                };
            }
            catch (Exception ex)
            {
                return CreateUpdateErrorResult($"An error occurred: {ex.Message}");
            }
        }

        public async Task<string> DeleteUserService(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID", nameof(userId));

            try
            {
                return await _masterRepository.DeleteUserRepo(userId);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while deleting the user.", ex);
            }
        }

        #endregion

        #region VALIDATION METHODS - Modular and Easy to Enable/Disable

        /// <summary>
        /// VALIDATION BLOCK 1: File Upload Basic Validations
        /// Comment out specific validations to disable them
        /// </summary>
        private ServiceResponse ValidateFileUpload(IFormFile file)
        {
            // Validation 1.1: File Null Check
            if (ENABLE_FILE_NULL_CHECK)
            {
                if (file == null || file.Length == 0)
                    return CreateResponse(true, "No file uploaded or empty file.", "400");
            }

            // Validation 1.2: File Size Check
            if (ENABLE_FILE_SIZE_VALIDATION)
            {
                if (file?.Length > MaxFileSize)
                    return CreateResponse(true, "File size exceeds the maximum limit of 5MB.", "400");
            }

            return null; // No validation errors
        }

        /// <summary>
        /// VALIDATION BLOCK 2: Role ID Validation
        /// </summary>
        private ServiceResponse ValidateRoleId(string role)
        {
            if (ENABLE_ROLE_ID_VALIDATION)
            {
                if (!int.TryParse(role, out int roleId))
                    return CreateResponse(true, "Invalid role ID format.", "400");
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 3: File Content Validations
        /// </summary>
        private async Task<ServiceResponse> ValidateFileContent(IFormFile file, bool isDealerFile = false)
        {
            if (ENABLE_FILE_CONTENT_VALIDATION)
            {
                // Generic file validation
                var validationResult = await _fileValidationService.ValidateFile(file);
                if ((bool)validationResult.isError)
                    return validationResult;

                // Specific content validation based on file type
                if (isDealerFile)
                {
                    var dealerValidation = await _fileValidationService.ValidateDealerExcel(file);
                    if (dealerValidation.isError == true)
                        return dealerValidation;
                }
                else
                {
                    var employeeValidation = await _fileValidationService.ValidateEmployeeExcel(file);
                    if (employeeValidation.isError == true)
                        return employeeValidation;
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 4: Excel Format Validation
        /// </summary>
        private ServiceResponse ValidateExcelFormat(IFormFile file)
        {
            if (ENABLE_FILE_EXTENSION_VALIDATION || ENABLE_FILE_SIGNATURE_VALIDATION)
            {
                if (!ValidateExcelFile(file, out string errorMessage))
                    return CreateResponse(true, errorMessage, "400");
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 5: Processed Data Validation
        /// </summary>
        private ServiceResponse ValidateProcessedData(int dataCount, string dataType)
        {
            if (ENABLE_EMPTY_DATA_CHECK)
            {
                if (dataCount == 0)
                    return CreateResponse(true, $"No valid {dataType} data found in the Excel file.", "400");
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 6: User ID Validation
        /// </summary>
        private UpdateResult ValidateUserId(int? userId)
        {
            if (ENABLE_USER_ID_VALIDATION)
            {
                if (!userId.HasValue)
                    return CreateUpdateErrorResult("UserId cannot be null");
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 7: Employee Name Validation
        /// </summary>
        private UpdateResult ValidateEmployeeName(string empName)
        {
            if (!string.IsNullOrEmpty(empName))
            {
                // Validation 7.1: Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(empName))
                        return CreateUpdateErrorResult("Employee name contains potentially malicious content");
                }

                // Validation 7.2: Alphanumeric Validation
                if (ENABLE_ALPHANUMERIC_VALIDATION)
                {
                    if (!_fileValidationService.IsAlphanumericWithSpace(empName))
                        return CreateUpdateErrorResult("Employee name must contain only letters, numbers and spaces");
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 8: Email Validation
        /// </summary>
        private UpdateResult ValidateEmailInput(string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                // Validation 8.1: Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(email))
                        return CreateUpdateErrorResult("Email contains potentially malicious content");
                }

                // Validation 8.2: Email Format Validation
                if (ENABLE_EMAIL_FORMAT_VALIDATION)
                {
                    if (!_utility.IsValidEmail(email))
                        return CreateUpdateErrorResult("Invalid email format");
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 9: Dealer Details Validation
        /// </summary>
        private UpdateResult ValidateDealerDetails(string empName, string location, string district, string zone, string state)
        {
            var parametersToValidate = new Dictionary<string, (string Value, string Name)>
            {
                { "EmpName", (empName, "Employee Name") },
                { "Location", (location, "Location") },
                { "District", (district, "District") },
                { "Zone", (zone, "Zone") },
                { "State", (state, "State") }
            };

            foreach (var param in parametersToValidate)
            {
                // Skip null values - they will remain unchanged
                if (string.IsNullOrWhiteSpace(param.Value.Value))
                    continue;

                // Validation 9.1: Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(param.Value.Value))
                        return CreateUpdateErrorResult($"{param.Value.Name} contains potentially malicious content");
                }

                // Validation 9.2: Alphanumeric Validation
                if (ENABLE_ALPHANUMERIC_VALIDATION)
                {
                    if (!_fileValidationService.IsAlphanumericWithSpace(param.Value.Value))
                        return CreateUpdateErrorResult($"{param.Value.Name} must contain only letters, numbers and spaces");
                }
            }

            return null;
        }

        #endregion

        #region EXISTING METHODS - Keeping original implementation

        public async Task<List<EmployeeModel>> ProcessEncryptedEmployeeExcelFromBlob(EncryptedBlobData blobData)
        {
            if (blobData == null)
                throw new ArgumentNullException(nameof(blobData));

            byte[] decryptedData = await DownloadAndDecryptBlobData(blobData);

            // Process the Excel file
            using var memoryStream = new MemoryStream(decryptedData);
            var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", "file.xlsx");

            return _utility.ParseExcelFile<EmployeeModel>(formFile, reader =>
            {
                var empNo = reader.GetValue(0)?.ToString()?.Trim();
                if (string.IsNullOrEmpty(empNo)) return null;

                return new EmployeeModel
                {
                    EmpNo = empNo,
                    Name = _fileValidationService.SanitizeInput(reader.GetValue(1)?.ToString()?.Trim()),
                    Email = _fileValidationService.SanitizeEmail(reader.GetValue(2)?.ToString()?.Trim() ?? $"{empNo}@mahindra.com"),
                    State = _fileValidationService.SanitizeInput(reader.GetValue(3)?.ToString()?.Trim())
                };
            });
        }

        public async Task<List<DealerModel>> ProcessEncryptedDealerExcelFromBlob(EncryptedBlobData blobData)
        {
            if (blobData == null)
                throw new ArgumentNullException(nameof(blobData));

            byte[] decryptedData = await DownloadAndDecryptBlobData(blobData);

            // Process the Excel file
            using var memoryStream = new MemoryStream(decryptedData);
            var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", "file.xlsx");

            return _utility.ParseExcelFile<DealerModel>(formFile, reader =>
            {
                var dealerNo = reader.GetValue(0)?.ToString()?.Trim();
                if (string.IsNullOrEmpty(dealerNo)) return null;

                return new DealerModel
                {
                    DealerNo = dealerNo,
                    Email = _fileValidationService.SanitizeEmail(reader.GetValue(1)?.ToString()?.Trim()),
                    DealershipName = _fileValidationService.SanitizeInput(reader.GetValue(2)?.ToString()?.Trim()),
                    District = _fileValidationService.SanitizeInput(reader.GetValue(3)?.ToString()?.Trim()),
                    Location = _fileValidationService.SanitizeInput(reader.GetValue(4)?.ToString()?.Trim()),
                    State = _fileValidationService.SanitizeInput(reader.GetValue(5)?.ToString()?.Trim()),
                    Zone = _fileValidationService.SanitizeInput(reader.GetValue(6)?.ToString()?.Trim()),
                    DateOfAppointment = _fileValidationService.SanitizeInput(reader.GetValue(7)?.ToString()?.Trim()),
                    DealershipAge = _fileValidationService.SanitizeInput(reader.GetValue(8)?.ToString()?.Trim()),
                    Industry = _fileValidationService.SanitizeInput(reader.GetValue(9)?.ToString()?.Trim()),
                    TRVolPlan = _fileValidationService.SanitizeInput(reader.GetValue(10)?.ToString()?.Trim()),
                    PlanVol = _fileValidationService.SanitizeInput(reader.GetValue(11)?.ToString()?.Trim()),
                    OwnFund = _fileValidationService.SanitizeInput(reader.GetValue(12)?.ToString()?.Trim()),
                    BGFund = _fileValidationService.SanitizeInput(reader.GetValue(13)?.ToString()?.Trim()),
                    SalesManpower = _fileValidationService.SanitizeInput(reader.GetValue(14)?.ToString()?.Trim()),
                    ServiceManpower = _fileValidationService.SanitizeInput(reader.GetValue(15)?.ToString()?.Trim()),
                    AdminManpower = _fileValidationService.SanitizeInput(reader.GetValue(16)?.ToString()?.Trim()),
                    ShowroomSize = _fileValidationService.SanitizeInput(reader.GetValue(17)?.ToString()?.Trim()),
                    WorkshopSize = _fileValidationService.SanitizeInput(reader.GetValue(18)?.ToString()?.Trim()),
                    TM = _fileValidationService.SanitizeInput(reader.GetValue(19)?.ToString()?.Trim()),
                    SCM = _fileValidationService.SanitizeInput(reader.GetValue(20)?.ToString()?.Trim()),
                    AM = _fileValidationService.SanitizeInput(reader.GetValue(21)?.ToString()?.Trim()),
                    CCM = _fileValidationService.SanitizeInput(reader.GetValue(22)?.ToString()?.Trim()),
                    CM = _fileValidationService.SanitizeInput(reader.GetValue(23)?.ToString()?.Trim()),
                    SH = _fileValidationService.SanitizeInput(reader.GetValue(24)?.ToString()?.Trim())
                };
            });
        }

        public bool ValidateExcelFile(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null)
            {
                errorMessage = "No file provided.";
                return false;
            }

            // Validation: File Extension Check
            if (ENABLE_FILE_EXTENSION_VALIDATION)
            {
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    errorMessage = "Invalid file format. Only Excel files (.xlsx, .xls) are allowed.";
                    return false;
                }

                // Validation: File Signature Check (only for .xlsx files)
                if (ENABLE_FILE_SIGNATURE_VALIDATION && extension == ".xlsx")
                {
                    var fileSignature = GetFileSignature(file);
                    if (!IsValidExcelFileSignature(fileSignature))
                    {
                        errorMessage = "Invalid Excel file signature.";
                        return false;
                    }
                }
            }

            return true;
        }

        public async Task<EncryptedBlobData> UploadEncryptedExcelToBlob(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file provided.", nameof(file));

            // Read file into memory
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] fileData = memoryStream.ToArray();

            // Encrypt the data
            var (encryptedData, key, iv) = _encryptionService.EncryptData(fileData);

            // Connect to Azure Blob Storage
            var cloudStorageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_containerName);

            // Ensure container exists
            await container.CreateIfNotExistsAsync();

            // Create unique filename and upload
            string uniqueFileName = $"encrypted_excel_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blockBlob = container.GetBlockBlobReference(uniqueFileName);

            // Set content type and upload
            blockBlob.Properties.ContentType = "application/octet-stream";

            await using (var encryptedStream = new MemoryStream(encryptedData))
            {
                await blockBlob.UploadFromStreamAsync(encryptedStream);
            }

            return new EncryptedBlobData
            {
                BlobUrl = blockBlob.Uri.ToString(),
                EncryptionKey = key,
                IV = iv
            };
        }

        public async Task<string> GenerateDownloadUrlAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            var blobUrl = await _masterRepository.GetBlobUrlByFileNameAsync(fileName);

            if (string.IsNullOrEmpty(blobUrl))
                throw new FileNotFoundException("File not found in the storage.");

            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var completeBLOB = $"{blobClient.StorageUri.PrimaryUri}{_containerName}/{blobUrl}";

            var blob = new CloudBlockBlob(new Uri(completeBLOB), blobClient.Credentials);

            if (!await blob.ExistsAsync())
                throw new FileNotFoundException("The blob does not exist in the storage.");

            var sasToken = _utility.GenerateSasToken(blob);

            return $"{blob.Uri}?{sasToken}";
        }

        #endregion

        #region Private Helper Methods

        private async Task<byte[]> DownloadAndDecryptBlobData(EncryptedBlobData blobData)
        {
            // Connect to Azure Blob Storage
            var cloudStorageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var blob = new CloudBlockBlob(new Uri(blobData.BlobUrl), blobClient.Credentials);

            // Download the encrypted data
            using var encryptedStream = new MemoryStream();
            await blob.DownloadToStreamAsync(encryptedStream);
            byte[] encryptedData = encryptedStream.ToArray();

            // Decrypt the data and return
            return _encryptionService.DecryptData(encryptedData, blobData.EncryptionKey, blobData.IV);
        }

        private byte[] GetFileSignature(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new BinaryReader(stream);

            stream.Seek(0, SeekOrigin.Begin); // Reset position to start
            var signature = reader.ReadBytes(4); // Read first 4 bytes for file signature

            stream.Seek(0, SeekOrigin.Begin); // Reset position for future reads

            return signature;
        }

        private bool IsValidExcelFileSignature(byte[] signature)
        {
            // .xlsx file starts with '50 4B 03 04' (ZIP format)
            return signature.Length >= 4 &&
                   signature[0] == 0x50 &&
                   signature[1] == 0x4B &&
                   signature[2] == 0x03 &&
                   signature[3] == 0x04;
        }

        private async Task DeleteBlobFile(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl))
                return;

            var cloudStorageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var blob = new CloudBlockBlob(new Uri(blobUrl), blobClient.Credentials);
            await blob.DeleteIfExistsAsync();
        }

        private ServiceResponse CreateResponse(bool isError, string message, string code)
        {
            return new ServiceResponse
            {
                isError = isError,
                Message = message,
                Status = isError ? "Failure" : "Success",
                Code = code
            };
        }

        private ServiceResponse CreateErrorResponse(Exception ex, string message)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = ex.Message,
                Message = message,
                Status = "Error",
                Code = "500"
            };
        }

        private UpdateResult CreateUpdateErrorResult(string message)
        {
            return new UpdateResult
            {
                RowsAffected = 0,
                Message = message,
                IsSuccess = false
            };
        }

        #endregion
    }

    #region VALIDATION CONFIGURATION GUIDE
    /*
     * ========================================
     * VALIDATION CONFIGURATION GUIDE
     * ========================================
     * 
     * This service uses modular validation approach. To enable/disable validations:
     * 
     * METHOD 1: Using Validation Flags (Recommended)
     * ----------------------------------------------
     * Change the boolean constants at the top of the class:
     * - Set to 'true' to enable validation
     * - Set to 'false' to disable validation
     * 
     * Example:
     * private const bool ENABLE_FILE_SIZE_VALIDATION = false; // Disables file size check
     * 
     * METHOD 2: Commenting Out Validation Blocks
     * ------------------------------------------
     * Comment out entire validation blocks in the main methods:
     * 
     * Example:
     * // VALIDATION BLOCK 1: File Upload Validations
     * // var fileValidationResult = ValidateFileUpload(file);
     * // if (fileValidationResult != null)
     * //     return fileValidationResult;
     * 
     * METHOD 3: Commenting Individual Validations
     * -------------------------------------------
     * Comment out specific validation checks within validation methods:
     * 
     * Example in ValidateFileUpload method:
     * // Validation 1.1: File Null Check
     * // if (ENABLE_FILE_NULL_CHECK)
     * // {
     * //     if (file == null || file.Length == 0)
     * //         return CreateResponse(true, "No file uploaded or empty file.", "400");
     * // }
     * 
     * VALIDATION BLOCKS AVAILABLE:
     * ============================
     * BLOCK 1: File Upload Basic Validations (file null, size)
     * BLOCK 2: Role ID Validation
     * BLOCK 3: File Content Validations (malicious content, format)
     * BLOCK 4: Excel Format Validation (extension, signature)
     * BLOCK 5: Processed Data Validation (empty data check)
     * BLOCK 6: User ID Validation
     * BLOCK 7: Employee Name Validation (malicious content, alphanumeric)
     * BLOCK 8: Email Validation (format, malicious content)
     * BLOCK 9: Dealer Details Validation (all dealer fields)
     * 
     * VALIDATION FLAGS AVAILABLE:
     * ===========================
     * File Upload Validations:
     * - ENABLE_FILE_NULL_CHECK
     * - ENABLE_FILE_SIZE_VALIDATION
     * - ENABLE_FILE_EXTENSION_VALIDATION
     * - ENABLE_FILE_SIGNATURE_VALIDATION
     * - ENABLE_FILE_CONTENT_VALIDATION
     * 
     * Data Input Validations:
     * - ENABLE_ROLE_ID_VALIDATION
     * - ENABLE_USER_ID_VALIDATION
     * - ENABLE_EMAIL_FORMAT_VALIDATION
     * - ENABLE_MALICIOUS_CONTENT_CHECK
     * - ENABLE_ALPHANUMERIC_VALIDATION
     * 
     * Business Logic Validations:
     * - ENABLE_EMPTY_DATA_CHECK
     * - ENABLE_DUPLICATE_CHECK (currently disabled, can be enabled)
     * 
     * QUICK DISABLE EXAMPLES:
     * =======================
     * 
     * 1. Disable file size validation:
     *    private const bool ENABLE_FILE_SIZE_VALIDATION = false;
     * 
     * 2. Disable email format validation:
     *    private const bool ENABLE_EMAIL_FORMAT_VALIDATION = false;
     * 
     * 3. Disable all malicious content checks:
     *    private const bool ENABLE_MALICIOUS_CONTENT_CHECK = false;
     * 
     * 4. Comment out entire validation block:
     *    // var fileValidationResult = ValidateFileUpload(file);
     *    // if (fileValidationResult != null)
     *    //     return fileValidationResult;
     * 
     * ADDING NEW VALIDATIONS:
     * ======================
     * 1. Add a new validation flag constant
     * 2. Create a new validation method following the pattern
     * 3. Add the validation call in the appropriate main method
     * 4. Update this documentation
     */
    #endregion
}