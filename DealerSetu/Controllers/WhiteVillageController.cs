using Microsoft.AspNetCore.Mvc;
using DealerSetu_Services.IServices;
using DealerSetu_Data.Models;
using DealerSetu_Data.Common;
using dealersetu_services.services;
using DealerSetu.Repository.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WhiteListingController : ControllerBase
    {
        private readonly IWhiteVillageService _whiteVillageService;
        private readonly JwtHelper _jwtHelper;
        private readonly Utility _utility;
        private readonly FileLoggerService _logger;

        public WhiteListingController(
            IWhiteVillageService whiteVillageService,
            JwtHelper jwtHelper,
            Utility utility)
        {
            _whiteVillageService = whiteVillageService;
            _jwtHelper = jwtHelper;
            _utility = utility;
            _logger = new FileLoggerService();
        }

        /// <summary>
        /// Retrieves the white-listed villages.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WhiteVillageModel>>> GetWhiteListing()
        {
            try
            {
                var result = await _whiteVillageService.GetWhiteListingService();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("WhiteListingController", "Error in GetWhiteListing", ex);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        /// <summary>
        /// Uploads an Excel file for white village data.
        /// </summary>
        [HttpPost("UploadWhiteVillageFile")]
        public async Task<IActionResult> UploadWhiteVillageFile(IFormFile excelFile, string stateId)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");

                // Call service to handle the file upload
                var result = await _whiteVillageService.UploadWhiteVillageFileService(excelFile, stateId, empNo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("WhiteListingController", "Error in UploadWhiteVillageFile", ex);
                return StatusCode(500, new { message = "An error occurred while uploading the white village file." });
            }
        }

        /// <summary>
        /// Retrieves the list of states.
        /// </summary>
        [HttpGet("GetStates")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var response = await _whiteVillageService.GetStateListService();

                if (response.isError == true)
                {
                    return StatusCode(500, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("WhiteListingController", "Error in GetRoles", ex);
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "An unexpected error occurred",
                    Status = "Error",
                    Code = "500"
                });
            }
        }

        /// <summary>
        /// Downloads a white village file.
        /// </summary>
        [HttpGet("DownloadWhiteVillageFiles")]
        public async Task<IActionResult> DownloadFormats(string fileName)
        {
            try
            {
                // Validate filename
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return BadRequest(new { error = "Filename cannot be empty or null." });
                }

                if (!_utility.IsValidFileName(fileName))
                {
                    return BadRequest(new { error = "Invalid filename format." });
                }

                var downloadUrl = await _whiteVillageService.WhiteVillageDownloadService(fileName);

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    return NotFound(new { error = "File not found or unavailable." });
                }

                return Ok(new { downloadUrl });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError("WhiteListingController", "Unauthorized access in DownloadFormats", ex);
                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("WhiteListingController", "Error in DownloadFormats", ex);
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }


        [HttpGet("ListWhiteVillageFiles")]
        public async Task<IActionResult> ListFiles()
        {
            try
            {
                var files = await _whiteVillageService.ListAllBlobsInContainer();
                return Ok(new { files });
            }
            catch (Exception ex)
            {
                _logger.LogError("WhiteListingController", "Error in ListFiles", ex);
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpDelete("DeleteWhiteVillageFile")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                // Validate filename
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return BadRequest(new { error = "Filename cannot be empty or null." });
                }
                if (!_utility.IsValidFileName(fileName))
                {
                    return BadRequest(new { error = "Invalid filename format." });
                }

                bool deleted = await _whiteVillageService.DeleteBlobFile(fileName);

                if (!deleted)
                {
                    return NotFound(new { error = "File not found in the storage." });
                }

                // Optionally: delete the file reference from your database too
                // await _whiteVillageRepository.DeleteWhiteVillageFileRecord(fileName);

                return Ok(new { success = true, message = $"File '{fileName}' successfully deleted." });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError("WhiteListingController", "Unauthorized access in DeleteFile", ex);
                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("WhiteListingController", "Error in DeleteFile", ex);
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }
}