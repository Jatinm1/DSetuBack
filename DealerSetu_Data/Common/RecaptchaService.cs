using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Common
{
    public class RecaptchaService
    {
        private readonly string _recaptchaSecretKey;
        private readonly HttpClient _httpClient;

        public RecaptchaService(IConfiguration configuration)
        {
            _recaptchaSecretKey = configuration["Recaptcha:SecretKey"];
            _httpClient = new HttpClient();
        }

        public async Task<bool> VerifyAsync(string recaptchaResponse)
        {
            try
            {
                var verificationUrl = "https://www.google.com/recaptcha/api/siteverify";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", _recaptchaSecretKey),
                    new KeyValuePair<string, string>("response", recaptchaResponse)
                });

                var response = await _httpClient.PostAsync(verificationUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    // Handle HTTP error
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic jsonData = JObject.Parse(responseContent);
                return (bool)jsonData.success;
            }
            catch (Exception ex)
            {
                // Handle exception
                //ExceptionFile.ExcepLog($"Exception during Recaptcha verification: {ex.ToString()}");
                return false;
            }
        }
    }
}
