using Cloudmersive.APIClient.NET.VirusScan.Api;
using Cloudmersive.APIClient.NET.VirusScan.Client;
using Cloudmersive.APIClient.NET.VirusScan.Model;
using DealerSetu.Repository.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Services.IServices;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace DealerSetu_Services.Services
{
    public class FileValidationService : IFileValidationService
    {
        private const long MaxFileSizeBytes = 4 * 1024 * 1024; // 4MB
        private const long MinFileSizeBytes = 1 * 1024; // 1KB
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string> { ".xlsx", ".xls" };
        private readonly Utility _utility;
        private static readonly string[] AllowedImageFormats = { "jpg", "jpeg", "png" };
        private static readonly string[] AllowedVideoFormats = { "mp4", "avi" };

        private static readonly Lazy<Dictionary<string, List<byte[]>>> FileSignatureMap = new(() =>
            new Dictionary<string, List<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                { "application/pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
                { "image/jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 } } },
                { "image/png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
                { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
                { "application/vnd.ms-excel", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } }
            });

        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex ScriptTagRegex = new Regex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HarmfulCharsRegex = new Regex(@"[\\:*?""<>|]", RegexOptions.Compiled);
        private static readonly Regex AlphanumericWithSpaceRegex = new Regex(@"^[a-zA-Z0-9\s()./&]+$", RegexOptions.Compiled);

        private static readonly string[] XssPatterns = {
            "onload=", "onerror=", "onmouseover=",
            "onfocus=", "onblur=", "onkeyup=",
            "onkeydown=", "onkeypress=", "onmouseout=",
            "onmousedown=", "onmousemove=", "onsubmit=",
            "onunload=", "onchange=", "ondblclick=",
            "alert(", "confirm(", "prompt(",
            "eval(", "function(", "return(",
            "settimeout(", "setinterval(", "new function("
        };

        private static readonly string[] ScriptPatterns = {
            "<script", "<?php", "<%", "<asp",
            "javascript:", "vbscript:", "data:",
            "<meta", "<iframe", "<object",
            "<embed", "<applet"
        };

        private static readonly string[] CommandPatterns = {
            "cmd.exe", "powershell", "bash",
            ":/bin/", ":/tmp/", ":/var/"
        };

        private static readonly string[] ExcelSuspiciousPatterns = {
            "=dde", "=system", "=exec",
            "auto_open", "auto_close", "workbook_open",
            "=cmd|", "=pow|", "=mshta|",
            "=http", "=ftp", "=file:",
            "javascript:", "vbscript:", "<!entity"
        };

        public FileValidationService(Utility utility)
        {
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
        }

        //HAVE TO ADD THIS AGAIN
        //COMMENTED BEACUSE CLOUDMERSIVE DOES NOT WORK ON LOCAL
        //*****************************************************************************************************
        //public async Task<ServiceResponse> ScanFileWithDefenderAsync(IFormFile file)
        //{
        //    var tempFilePath = Path.GetTempFileName();
        //    try
        //    {
        //        // Save IFormFile to temp file
        //        using (var stream = new FileStream(tempFilePath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //        // Set API Key
        //        Configuration.Default.AddApiKey("Apikey", "Mykey");

        //        var apiInstance = new ScanApi();
        //        using (var fs = System.IO.File.OpenRead(tempFilePath))
        //        {
        //            var result = apiInstance.ScanFile(fs);

        //            if (result.FoundViruses != null && result.FoundViruses.Count > 0)
        //            {
        //                // Extract virus names for message
        //                var virusNames = result.FoundViruses.Select(v => v.VirusName).ToList();
        //                var virusListString = string.Join(", ", virusNames);

        //                return CreateErrorResponse(
        //                    "Malicious file detected",
        //                    "MALICIOUS_FILE_DETECTED",
        //                    $"Virus(es) found: {virusListString}"
        //                );
        //            }

        //            // Clean file
        //            return CreateSuccessResponse(file);
        //        }
        //    }
        //    finally
        //    {
        //        if (System.IO.File.Exists(tempFilePath))
        //        {
        //            System.IO.File.Delete(tempFilePath);
        //        }
        //    }
        //}     



        //HAVE TO ADD THIS AGAIN
        //COMMENTED BEACUSE CLOUDMERSIVE DOES NOT WORK ON LOCAL
        //**************************************************************************************************
        // Step 3: Check the Malicious Content
        //try
        //{
        //    // Scan the file directly without conversion
        //    var maliciousCheck = await ScanFileWithDefenderAsync(imageFile);
        //    if ((bool)maliciousCheck.isError)
        //    {
        //        response.isError = true;
        //        response.Message = "File is not Correct";
        //        response.Code = "302";
        //        response.Status = "Error";
        //        return response;
        //    }
        //}
        //catch (Exception ex)
        //{
        //    response.isError = true;
        //    response.Message = "Invalid request. File Upload Limit Reached. Please try again later.";
        //    response.Code = "310";
        //    response.Status = "Error";
        //    return response;
        //}



        public async Task<ServiceResponse> ValidateFile(IFormFile file)
        {
            if (file == null)
            {
                return CreateErrorResponse("No file provided", "FILE_NULL", "No file was provided for validation");
            }

            try
            {
                var basicValidation = ValidateBasicFileProperties(file);
                if ((bool)basicValidation.isError) return basicValidation;

                if (!await ValidateFileSignatureAsync(file))
                {
                    return CreateErrorResponse("File content does not match its declared type",
                        "INVALID_FILE_SIGNATURE", "The file content appears to be modified or corrupted");
                }

                var contentSecurityResponse = await ValidateFileContentAsync(file);
                if ((bool)contentSecurityResponse.isError) return contentSecurityResponse;

                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                //if (AllowedExtensions.Contains(extension))
                //{
                //    var excelValidation = await ValidateExcelStructureAsync(file);
                //    if ((bool)excelValidation.isError) return excelValidation;

                //    return await ScanFileWithDefenderAsync(file);
                //}

                return CreateSuccessResponse(file);
            }
            catch (Exception ex)
            {
                return CreateExceptionResponse(ex);
            }
        }

        public bool ValidateImageFile(IFormFile file)
        {
            return ValidateFile(file, AllowedImageFormats);
        }

        public static bool ValidateVideoFile(IFormFile file)
        {
            return ValidateFile(file, AllowedVideoFormats);
        }

        private static bool ValidateFile(IFormFile file, string[] allowedFormats)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
            {
                return false;
            }

            string reqExtension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
            string evaluatedType = MagicNumberClass.MagicNumber(file);
            string[] evaluatedTypes = evaluatedType.Split('/');

            bool isValidFileType = evaluatedTypes.Contains(reqExtension, StringComparer.OrdinalIgnoreCase) &&
                                   allowedFormats.Contains(reqExtension, StringComparer.OrdinalIgnoreCase);

            return isValidFileType;
        }

        public async Task<ServiceResponse> ValidateImageAsync(IFormFile imageFile, long maxFileSize)
        {
            var response = new ServiceResponse();

            if (imageFile.Length > maxFileSize)
            {
                response.isError = true;
                response.Message = "Max File Size can be 20 MB";
                response.Code = "413";
                response.Status = "Error";
                return response;
            }

            if (!ValidateImageFile(imageFile))
            {
                response.isError = true;
                response.Message = "Invalid file type. Only JPG, JPEG, PNG files are allowed.";
                response.Code = "302";
                response.Status = "Error";
                return response;
            }

            response.isError = false;
            response.Status = "Success";
            response.Message = "File validation successful";
            return response;
        }

        public async Task<ServiceResponse> ValidateEmployeeExcel(IFormFile file)
        {
            return await ValidateExcelFileAsync(file, ValidateEmployeeDataRow, GetEmployeeRequiredColumns());
        }

        public async Task<ServiceResponse> ValidateDealerExcel(IFormFile file)
        {
            return await ValidateExcelFileAsync(file, ValidateDealerDataRow, GetDealerRequiredColumns());
        }

        private async Task<ServiceResponse> ValidateExcelFileAsync(
            IFormFile file,
            Func<DataRow, int, List<string>, Dictionary<string, bool>, ServiceResponse> rowValidator,
            Dictionary<string, bool> requiredColumns)
        {
            if (file == null)
            {
                return CreateErrorResponse("No file provided", "FILE_NULL", "No file was provided for validation");
            }

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using var stream = file.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateReader(stream);

                if (reader == null)
                {
                    return CreateErrorResponse("Excel file is empty or corrupted", "EXCEL_CORRUPT", "Unable to read Excel file");
                }

                var result = reader.AsDataSet();
                if (result.Tables.Count == 0)
                {
                    return CreateErrorResponse("Excel file contains no worksheets", "NO_WORKSHEETS", "The Excel file has no data");
                }

                var dataTable = result.Tables[0];
                if (dataTable.Rows.Count < 2)
                {
                    return CreateErrorResponse("Excel file is empty", "NO_DATA", "The Excel file contains no data rows");
                }

                var headers = GetHeaders(dataTable);
                var headerValidation = ValidateHeaders(headers, requiredColumns);
                if ((bool)headerValidation.isError) return headerValidation;

                for (int rowIndex = 1; rowIndex < dataTable.Rows.Count; rowIndex++)
                {
                    var rowValidation = rowValidator(dataTable.Rows[rowIndex], rowIndex + 1, headers, requiredColumns);
                    if ((bool)rowValidation.isError) return rowValidation;
                }

                return CreateExcelSuccessResponse(dataTable.Rows.Count - 1);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Excel validation failed: {ex.Message}", "EXCEL_VALIDATION_ERROR", "Failed to validate Excel file");
            }
        }

        private List<string> GetHeaders(DataTable dataTable)
        {
            var headers = new List<string>(dataTable.Columns.Count);
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                headers.Add(dataTable.Rows[0][i].ToString().Trim());
            }
            return headers;
        }

        private ServiceResponse ValidateEmployeeDataRow(DataRow row, int rowNumber, List<string> headers, Dictionary<string, bool> requiredColumns)
        {
            var requiredFields = new[] { "EmpNo", "Name", "Email" };
            foreach (var field in requiredFields)
            {
                var fieldIndex = headers.IndexOf(field);
                if (fieldIndex >= 0 && string.IsNullOrWhiteSpace(row[fieldIndex].ToString()))
                {
                    return CreateRowErrorResponse(field, rowNumber, $"{field} cannot be empty");
                }
            }

            for (int i = 0; i < headers.Count; i++)
            {
                var columnName = headers[i];
                if (columnName.Equals("Email", StringComparison.OrdinalIgnoreCase))
                    continue;

                var cellValue = row[i].ToString().Trim();
                if (!string.IsNullOrEmpty(cellValue) && !AlphanumericWithSpaceRegex.IsMatch(cellValue))
                {
                    return CreateRowErrorResponse(columnName, rowNumber,
                        $"{columnName} must contain only alphanumeric characters and spaces");
                }
            }

            var emailIndex = headers.IndexOf("Email");
            if (emailIndex >= 0)
            {
                var email = row[emailIndex].ToString().Trim();
                if (!string.IsNullOrEmpty(email) && !EmailRegex.IsMatch(email))
                {
                    return CreateRowErrorResponse("Email", rowNumber, "Invalid email format");
                }
            }

            return CreateSuccessResponse();
        }

        private ServiceResponse ValidateDealerDataRow(DataRow row, int rowNumber, List<string> headers, Dictionary<string, bool> requiredColumns)
        {
            var requiredFields = new[] { "DealerNo", "Email", "DealershipName", "TM", "SCM", "AM", "CCM", "CM", "SH" };

            foreach (var field in requiredFields)
            {
                var fieldIndex = headers.IndexOf(field);
                if (fieldIndex >= 0 && string.IsNullOrWhiteSpace(row[fieldIndex].ToString()))
                {
                    return CreateRowErrorResponse(field, rowNumber, $"{field} cannot be empty");
                }
            }

            for (int i = 0; i < headers.Count; i++)
            {
                var columnName = headers[i];
                if (columnName.Equals("Email", StringComparison.OrdinalIgnoreCase))
                    continue;

                var cellValue = row[i].ToString().Trim();
                if (!string.IsNullOrEmpty(cellValue) && !AlphanumericWithSpaceRegex.IsMatch(cellValue))
                {
                    return CreateRowErrorResponse(columnName, rowNumber,
                        $"{columnName} must contain only alphanumeric characters and spaces");
                }
            }

            var emailIndex = headers.IndexOf("Email");
            if (emailIndex >= 0)
            {
                var email = row[emailIndex].ToString().Trim();
                if (!string.IsNullOrEmpty(email) && !EmailRegex.IsMatch(email))
                {
                    return CreateRowErrorResponse("Email", rowNumber, "Invalid email format");
                }
            }

            return CreateSuccessResponse();
        }

        private Dictionary<string, bool> GetEmployeeRequiredColumns()
        {
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                { "EmpNo", true },
                { "Name", true },
                { "Email", true },
                { "State", false }
            };
        }

        private Dictionary<string, bool> GetDealerRequiredColumns()
        {
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                { "DealerNo", true },
                { "Email", true },
                { "DealershipName", true },
                { "Location", false },
                { "District", false },
                { "State", false },
                { "Zone", false },
                { "DateOfAppointment", false },
                { "DealershipAge", false },
                { "Industry", false },
                { "TRVolPlan", false },
                { "PlanVol", false },
                { "OwnFund", false },
                { "BGFund", false },
                { "SalesManpower", false },
                { "ServiceManpower", false },
                { "AdminManpower", false },
                { "ShowroomSize", false },
                { "WorkshopSize", false },
                { "TM", true },
                { "SCM", true },
                { "AM", true },
                { "CCM", true },
                { "CM", true },
                { "SH", true }
            };
        }

        private ServiceResponse ValidateHeaders(List<string> headers, Dictionary<string, bool> requiredColumns)
        {
            var missingRequired = requiredColumns
                .Where(rc => rc.Value)
                .Where(rc => !headers.Contains(rc.Key, StringComparer.OrdinalIgnoreCase))
                .Select(rc => rc.Key)
                .ToList();

            if (missingRequired.Count > 0)
            {
                return CreateErrorResponse(
                    $"Missing mandatory columns: {string.Join(", ", missingRequired)}",
                    "MISSING_COLUMNS",
                    "The Excel file is missing required columns");
            }

            return CreateSuccessResponse();
        }

        private ServiceResponse ValidateBasicFileProperties(IFormFile file)
        {
            if (file.Length == 0)
            {
                return CreateErrorResponse("Empty file", "FILE_EMPTY", "The uploaded file is empty");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return CreateErrorResponse(
                    $"File size exceeds maximum limit of {MaxFileSizeBytes / 1024 / 1024}MB",
                    "FILE_SIZE_EXCEEDED",
                    "The uploaded file is too large");
            }

            if (file.Length < MinFileSizeBytes)
            {
                return CreateErrorResponse(
                    "File size is suspiciously small",
                    "FILE_SIZE_TOO_SMALL",
                    "The uploaded file is too small to be valid");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return CreateErrorResponse(
                    $"Invalid file type. Allowed extensions are: {string.Join(", ", AllowedExtensions)}",
                    "INVALID_FILE_TYPE",
                    "The file type is not supported");
            }

            return CreateSuccessResponse();
        }

        private async Task<ServiceResponse> ValidateFileContentAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            string content = await reader.ReadToEndAsync();

            if (ContainsMaliciousPatterns(content))
            {
                return CreateErrorResponse(
                    "Malicious content detected",
                    "MALICIOUS_CONTENT",
                    "The file contains potentially harmful content");
            }

            return CreateSuccessResponse();
        }

        public async Task<List<T>> ParseExcelFile<T>(IFormFile file, Func<IExcelDataReader, T> mapRow) where T : class, new()
        {
            var validationResult = await ValidateFile(file);
            if ((bool)validationResult.isError)
            {
                throw new InvalidOperationException(validationResult.Message);
            }

            var result = new List<T>();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = file.OpenReadStream())
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    while (reader.Read())
                    {
                        var item = mapRow(reader);
                        if (item != null)
                        {
                            result.Add(item);
                        }
                    }
                } while (reader.NextResult());
            }

            return result;
        }

        private async Task<ServiceResponse> ValidateExcelStructureAsync(IFormFile file)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var stream = file.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateReader(stream);

                if (reader == null || reader.FieldCount == 0)
                {
                    return CreateErrorResponse(
                        "Invalid Excel structure",
                        "INVALID_EXCEL_STRUCTURE",
                        "The Excel file appears to be corrupted or empty");
                }

                if (await ContainsSuspiciousExcelContentAsync(reader))
                {
                    return CreateErrorResponse(
                        "Suspicious Excel content detected",
                        "SUSPICIOUS_EXCEL_CONTENT",
                        "The Excel file contains potentially harmful content");
                }

                return CreateSuccessResponse();
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(
                    ex.Message,
                    "EXCEL_VALIDATION_ERROR",
                    "Failed to validate Excel structure");
            }
        }

        private async Task<bool> ValidateFileSignatureAsync(IFormFile file)
        {
            if (!FileSignatureMap.Value.TryGetValue(file.ContentType, out var signatures))
                return false;

            using var reader = new BinaryReader(file.OpenReadStream());
            var longestSignature = signatures.Max(s => s.Length);
            var headerBytes = reader.ReadBytes(longestSignature);

            return signatures.Any(signature =>
                headerBytes.Length >= signature.Length &&
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }

        public bool ContainsMaliciousPatterns(string content)
        {
            var normalizedContent = content.ToLowerInvariant();
            return CheckPatterns(normalizedContent, XssPatterns) ||
                   CheckPatterns(normalizedContent, ScriptPatterns) ||
                   CheckPatterns(normalizedContent, CommandPatterns);
        }

        private bool CheckPatterns(string content, string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                if (content.Contains(pattern))
                {
                    return true;
                }
            }
            return false;
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sanitizedInput = HarmfulCharsRegex.Replace(input, "");
            sanitizedInput = ScriptTagRegex.Replace(input, "");

            return sanitizedInput.Trim();
        }

        public string SanitizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            email = email.Trim();

            if (_utility.IsValidEmail(email))
            {
                return HttpUtility.HtmlEncode(email);
            }

            return string.Empty;
        }

        private async Task<bool> ContainsSuspiciousExcelContentAsync(IExcelDataReader reader)
        {
            do
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string cellValue = reader.GetValue(i)?.ToString()?.ToLower() ?? string.Empty;
                        if (CheckPatterns(cellValue, ExcelSuspiciousPatterns))
                        {
                            return true;
                        }
                    }
                }
            } while (reader.NextResult());

            return false;
        }

        public bool IsAlphanumericWithSpace(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;
            string decoded = WebUtility.HtmlDecode(input);
            return Regex.IsMatch(decoded, @"^[a-zA-Z0-9\s()./&-]+$");
        }

        private ServiceResponse CreateErrorResponse(string error, string code, string message)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = error,
                Status = "Failed",
                Code = code,
                Message = message
            };
        }

        private ServiceResponse CreateRowErrorResponse(string column, int rowNumber, string message)
        {
            return new ServiceResponse
            {
                isError = true,
                Message = $"{message} at row {rowNumber}",
                Error = $"Column: {column}, Row: {rowNumber}",
                Status = "Error"
            };
        }

        private ServiceResponse CreateExceptionResponse(Exception ex)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = ex.Message,
                Status = "Error",
                Code = "FILE_VALIDATION_EXCEPTION",
                Message = "An unexpected error occurred during file validation",
                result = new { stackTrace = ex.StackTrace }
            };
        }

        private ServiceResponse CreateSuccessResponse()
        {
            return new ServiceResponse
            {
                isError = false,
                Status = "Success"
            };
        }

        private ServiceResponse CreateSuccessResponse(IFormFile file)
        {
            return new ServiceResponse
            {
                isError = false,
                Status = "Success",
                Code = "FILE_VALIDATION_SUCCESS",
                Message = "File validation completed successfully",
                result = new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    contentType = file.ContentType,
                    validationTimestamp = DateTime.UtcNow
                }
            };
        }

        private ServiceResponse CreateExcelSuccessResponse(int rowCount)
        {
            return new ServiceResponse
            {
                isError = false,
                Status = "Success",
                Message = "File validated successfully",
                totalCount = rowCount
            };
        }
    }

    public class DefenderScanResult
    {
        public string InfectedFiles { get; set; }
        public string Message { get; set; }
        public bool IsClean { get; set; }
    }
}