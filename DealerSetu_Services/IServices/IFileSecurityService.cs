using DealerSetu_Data.Models.HelperModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface IFileSecurityService
    {

        Task<ServiceResponse> ValidateContent(string content);
        Task<ServiceResponse> ValidateFile(IFormFile file);
        string NormalizeContent(string content);
        bool ContainsSqlInjection(string content);
        bool ContainsXss(string content);

    }
}
