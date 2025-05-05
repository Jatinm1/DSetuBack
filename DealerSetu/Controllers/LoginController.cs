using Microsoft.AspNetCore.Mvc;
using DealerSetu_Services.IServices;
using DealerSetu_Services.Services;
using DealerSetu_Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _loginService;
        private readonly IConfiguration _configuration;
        private readonly JwtHelper _jwtHelper;
        private readonly RSAEncryptionService _rsaKeyService;
        private readonly ValidationHelper _validationHelper;
        private readonly RecaptchaService _recaptchaService;
        public LoginController(ILoginService loginService, IConfiguration configuration, JwtHelper jwtHelper, RSAEncryptionService rsaKeyService, ValidationHelper validationHelper, RecaptchaService recaptchaService)
        {
            _loginService = loginService;
            _configuration = configuration;
            _jwtHelper = jwtHelper;
            _rsaKeyService = rsaKeyService;
            _validationHelper = validationHelper;
            _recaptchaService = recaptchaService;
        }


        [HttpPost("LoginUser")]
        public async Task<IActionResult> LoginUser([FromBody] EncryptedPayload payload)
        {
            try
            {
                var modelStateValidation = _validationHelper.ValidateModelState(ModelState);
                if (modelStateValidation != null)
                    return BadRequest(modelStateValidation);

                var payloadValidation = _validationHelper.ValidateLoginPayload(payload);
                if (payloadValidation != null)
                    return BadRequest(payloadValidation);

                // Decrypt AES key and IV
                string decryptedKeyString;
                string decryptedIvString;
                try
                {
                    decryptedKeyString = _rsaKeyService.DecryptRSA(payload.EncryptedKey);
                    decryptedIvString = _rsaKeyService.DecryptRSA(payload.EncryptedIV);
                }
                catch (CryptographicException)
                {
                    return BadRequest(new ServiceResponse
                    {
                        Status = "Failure",
                        Code = "400",
                        Message = "Failed to decrypt key/IV"
                    });
                }

                byte[] aesKey = Convert.FromBase64String(decryptedKeyString);
                byte[] aesIv = Convert.FromBase64String(decryptedIvString);

                var keyValidation = _validationHelper.ValidateAESKeyAndIV(aesKey, aesIv);
                if (keyValidation != null)
                    return BadRequest(keyValidation);

                try
                {
                    byte[] encryptedDataBytes = Convert.FromBase64String(payload.EncryptedData);
                    string decryptedJson = AESDecryption.DecryptAES(encryptedDataBytes, aesKey, aesIv);

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var loginModel = JsonSerializer.Deserialize<LoginModel>(decryptedJson, options);

                    var loginModelValidation = _validationHelper.ValidateLoginModel(loginModel);
                    if (loginModelValidation != null)
                        return BadRequest(loginModelValidation);

                    //************************************RECAPTHA CHECK************************************
                    //*******************************COMMENTED FOR LOCAL USE********************************

                    //if (!await _recaptchaService.VerifyAsync(payload.reCaptcha))
                    //{
                    //    return Ok(new { Message = "reCAPTCHA Verification Failed.", Status = 500 });
                    //}

                    //var result = await _loginService.Login_Service(loginModel); // Local
                    var result = await _loginService.LDAPLoginService(loginModel); // LDAP
                    return Ok(result);


                }
                catch (Exception ex)
                {
                    return BadRequest(new ServiceResponse
                    {
                        Status = "Failure",
                        Code = "400",
                        Message = "Failed to process login data"
                    });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new ServiceResponse
                {
                    Status = "Failure",
                    Code = "500",
                    Message = "An internal server error occurred"
                });
            }
        }

        //************************************LOGIN METHOD WITHOUT ENCRYPTION************************************


        //[HttpPost("LoginUser")]
        //public IActionResult LoginUser([FromBody] LoginModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    try
        //    {
        //        var objModel = new LoginModel
        //        {
        //            EmpNo = model.EmpNo?.Trim(), // Prevent whitespace-based bypass
        //            Password = model.Password
        //        };

        //        ServiceResponse objUserSession = _loginService.Login_Service(objModel);

        //        return Ok(objUserSession);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error
        //        return StatusCode(500, ex.Message);
        //    }
        //}

        [HttpPost("LogoutUser")]
        public async Task<IActionResult> LogOut(LogoutModel logoutModel)
        {
            var result = await _loginService.LogOutService(logoutModel.empNo);
            if (result.Code != "200")
                return BadRequest(new { message = result.Message });

            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("isAuthenticated");
            return Ok(result);
        }

        //************************************ORIGINAL HEARTBEAT IMPLEMENTATION************************************

        //[HttpPost("HeartBeat")]
        //public async Task<ActionResult<bool>> HeartBeat([FromBody] HeartbeatRequestModel request)
        //{
        //    try
        //    {
        //        var result = await _loginService.HeartBeatAsync(request.EmpNo);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex.Message);
        //    }
        //}

        //[HttpGet("PendingCount")]
        //public async Task<IActionResult> PendingCount()
        //{
        //    try
        //    {

        //        var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
        //        var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

        //        var filter = new FilterModel
        //        {
        //            EmpNo = empNo,
        //            RoleId = roleId
        //        };
        //        var response = await _loginService.PendingCountService(filter);

        //        if (response.isError == true)
        //        {
        //            return StatusCode(500, response);
        //        }

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new ServiceResponse
        //        {
        //            isError = true,
        //            Error = ex.Message,
        //            Message = "An unexpected error occurred",
        //            Status = "Error",
        //            Code = "500"
        //        });
        //    }
        //}

        [HttpPost("LoginHeartBeat")]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Consumes("application/json")] // Optional, since there's no body
        public async Task<ActionResult<bool>> LoginHeartBeat([FromBody] HeartbeatRequestModel request)
        {
            try
            {                         
                var result = await _loginService.UpdateLoginHeartbeatService(request.EmpNo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("RegularHeartBeat")]

        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [Consumes("application/json")] // Optional, since there's no body

        public async Task<ActionResult<bool>> RegularHeartBeat([FromBody] HeartbeatRequestModel request)
        {
            try
            {               
                var result = await _loginService.UpdateRegularHeartbeatService(request.EmpNo);
                return Ok(result);
            }
            catch (Exception ex)

            {
                return StatusCode(500, ex.Message);
            }

        }

    }
}
