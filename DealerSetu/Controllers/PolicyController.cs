using DealerSetu.Repository.Common;
using DealerSetu_Data.Models;
using DealerSetu_Data.Models.HelperModels;
using dealersetu_services.services;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Mvc;
namespace DealerSetu.Controllers
{
    public class PolicyController : Controller
    {
        private IPolicyService _policyService;
        private readonly IConfiguration _configuration;
        private readonly IMasterService _masterService;
        private readonly Utility _utility;
        private readonly HttpClient _httpClient;
        private static readonly HttpClient client = new HttpClient();
        private readonly FileLoggerService _logger;

        public PolicyController(IPolicyService policyService, IConfiguration configuration, IMasterService masterService, Repository.Common.Utility utility, HttpClient httpClient)
        {
            _policyService = policyService;
            _configuration = configuration;
            _masterService = masterService;
            _utility = utility;
            _httpClient = httpClient;
            _logger = new FileLoggerService();
        }

        [HttpGet("GetPolicyList")]
        public IActionResult GetPolicyList()
        {
            try
            {
                var result = _policyService.GetPolicyListService();
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("PolicyController", "Error in GetPolicyList", ex);
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost("UploadPolicyPdf")]
        public async Task<IActionResult> SendFiletoServer([FromForm] PolicyUploadModel model, int RAId)
        {
            try
            {
                // Call the service to handle file upload
                var result = await _policyService.SendFiletoServerService(model, RAId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("PolicyController", "Error in UploadPolicyPdf", ex);
                return StatusCode(500, new { error = "An error occurred while uploading the policy PDF.", details = ex.Message });
            }
        }

        [HttpGet("ViewFile")]
        public async Task<IActionResult> ViewFile(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return BadRequest(new { error = "Filename cannot be empty or null." });
                }
                // Validate the filename format
                if (!_utility.IsValidFileName(fileName))
                {
                    return BadRequest(new { error = "Invalid filename format." });
                }
                // Generate download URL
                var downloadUrl = await _masterService.GenerateDownloadUrlAsync(fileName);
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    return NotFound(new { error = "File not found or unavailable." });
                }
                var response = await _httpClient.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { error = "Failed to retrieve file" });
                }
                var content = await response.Content.ReadAsByteArrayAsync();
                // Determine file content type
                string contentType = _utility.GetContentType(fileName);
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
                return File(content, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError("PolicyController", "Error in ViewFile", ex);
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }
}
//*************************************Add this Method above to Upload New Polices*************************************
//[HttpPost("UploadPolicyPdf")]
//public async Task<IActionResult> SendFiletoServer([FromForm] PolicyUploadModel model, int RAId)
//{
//    // Call the service to handle file upload
//    var result = await _policyService.SendFiletoServerService(model, RAId);
//    return Ok(result);
//}