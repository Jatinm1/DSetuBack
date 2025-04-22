using DealerSetu_Data.Models.HelperModels;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;

namespace DealerSetu.Repository.Common
{
    public class Utility
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public static readonly Regex EmailRegex = new Regex(
    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public Utility(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Logs exceptions to a text file
        public void ExcepLog(Exception ex)
        {
            try
            {
                // Get the current HttpContext
                var context = _httpContextAccessor.HttpContext;

                // Construct the directory and file path
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogException");
                string logFilePath = Path.Combine(logDirectory, "LogFile.txt");

                // Ensure the directory exists
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Append exception details to the log file
                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    sw.WriteLine("===== Error Log =====");
                    sw.WriteLine("=== Error Occurred on === " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("Error Message: " + ex.Message);
                    sw.WriteLine("Stack Trace: " + ex.StackTrace);
                    sw.WriteLine("==============================================");
                    sw.WriteLine("");
                }
            }
            catch (Exception loggingEx)
            {
                // Log to a fallback mechanism like Debug
                Debug.WriteLine("Error logging exception: " + loggingEx.Message);
            }
        }

        // Parses Excel file and maps rows to specified type
        public List<T> ParseExcelFile<T>(IFormFile file, Func<IExcelDataReader, T> mapRow) where T : class, new()
        {
            var result = new List<T>();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = file.OpenReadStream())
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                // Skip header row
                if (reader.Read())
                {
                    while (reader.Read())
                    {
                        var item = mapRow(reader);
                        if (item != null)
                        {
                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }

        // Validates filename format
        public bool IsValidFileName(string fileName)
        {
            // Allow alphanumeric, underscores, hyphens, and periods
            return Regex.IsMatch(fileName, @"^[a-zA-Z0-9_.\s-]+$");
        }

        // Validates username format
        public bool IsValidUsername(string username)
        {
            // Only allow alphanumeric characters and underscore
            return !string.IsNullOrEmpty(username) &&
                   username.Length <= 50 &&
                   Regex.IsMatch(username, "^[a-zA-Z0-9_]+$");
        }    

        // Sanitizes URL input
        public string SanitizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;

            url = HttpUtility.HtmlEncode(url); // Protect against HTML injection
            return url;
        }

        // Generates a time-limited SAS token for blob access
        public string GenerateSasToken(CloudBlockBlob blob)
        {
            // Define the SAS token's permissions and expiry
            var sasPolicy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read, // Allow read access only
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(2) // Token valid for 2 minutes
            };

            // Generate the SAS token
            return blob.GetSharedAccessSignature(sasPolicy);
        }

        // Gets the MIME content type based on file extension
        public string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        // Validates email format
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim();

            // First check with regex for basic format
            if (!EmailRegex.IsMatch(email))
                return false;

            // Then try to create a MailAddress object for additional validation
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }




        /// <summary>
        /// Validates if the provided fiscal year is valid based on business rules
        /// </summary>
        /// <param name="fy">Fiscal year to validate</param>
        /// <param name="errorMessage">Output error message if validation fails</param>
        /// <returns>True if fiscal year is valid, otherwise false</returns>
        public bool IsValidFiscalYear(int fy, out string errorMessage)
        {
            errorMessage = null;

            // Validate data type
            if (fy.GetType() != typeof(int))
            {
                errorMessage = "Invalid fiscal year data type. Please provide a valid integer value.";
                return false;
            }

            // Validate length (4 digits)
            if (fy.ToString().Length != 4)
            {
                errorMessage = "Fiscal year must be a valid 4-digit number.";
                return false;
            }

            // Validate positive value
            if (fy <= 0)
            {
                errorMessage = "Fiscal year must be a positive integer.";
                return false;
            }

            // Validate range
            int currentYear = DateTime.Now.Year;
            if (fy < 2000 || fy > currentYear)
            {
                errorMessage = $"Fiscal year must be between 2000 and {currentYear}.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a standardized error response object
        /// </summary>
        /// <param name="error">Error title</param>
        /// <param name="message">Detailed error message</param>
        /// <param name="code">HTTP status code as string</param>
        /// <returns>ServiceResponse with error details</returns>
        public ServiceResponse CreateErrorResponse(string error, string message, string code)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = error,
                Message = message,
                Code = code,
                Status = "Error"
            };
        }

        /// <summary>
        /// Creates a standardized success response object
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="result">Result data object</param>
        /// <returns>ServiceResponse with success details</returns>
        public ServiceResponse CreateSuccessResponse(string message, object result = null)
        {
            return new ServiceResponse
            {
                isError = false,
                Error = null,
                Message = message,
                Code = "200",
                Status = "Success",
                result = result
            };
        }

        /// <summary>
        /// Returns a standardized internal server error response
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <param name="exception">Exception that occurred</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="methodName">Name of the method where exception occurred</param>
        /// <returns>ActionResult with 500 status code</returns>
        public async Task<IActionResult> HandleInternalServerError<T>(
            ControllerBase controller,
            Exception exception,
            ILogger<T> logger,
            string methodName = null)
        {
            // Log the exception
            logger.LogError(exception, $"Error in {methodName ?? "controller method"}");

            // Return standard 500 response
            return controller.StatusCode(500, new ServiceResponse
            {
                isError = true,
                Error = "Internal Server Error",
                Message = "An unexpected error occurred. Please try again later.",
                Code = "500",
                Status = "Error"
            });
        }

        /// <summary>
        /// Validates a string input for potential security concerns
        /// </summary>
        /// <param name="input">String to validate</param>
        /// <param name="maxLength">Maximum allowed length</param>
        /// <param name="errorMessage">Output error message if validation fails</param>
        /// <returns>True if input is valid, otherwise false</returns>
        public bool ValidateStringInput(string input, int maxLength, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                return true; // Empty strings are considered valid (optional fields)
            }

            if (input.Length > maxLength)
            {
                errorMessage = $"Input exceeds maximum length of {maxLength} characters.";
                return false;
            }

            // Add additional validation rules as needed (e.g., check for SQL injection patterns)

            return true;
        }
    }
}