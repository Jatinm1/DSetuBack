using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.HelperModels;


namespace DealerSetu_Services.IServices
{
    public interface IFileValidationService
    {
        Task<ServiceResponse> ValidateFile(IFormFile file);
        Task<ServiceResponse> ValidateEmployeeExcel(IFormFile file);
        Task<ServiceResponse> ValidateDealerExcel(IFormFile file);
        public string SanitizeInput(string input);
        public string SanitizeEmail(string email);
        Task<List<T>> ParseExcelFile<T>(IFormFile file, Func<IExcelDataReader, T> mapRow) where T : class, new();

        bool ContainsMaliciousPatterns(string content);
        //bool IsValidEmail(string email);
        bool IsAlphanumericWithSpace(string input);
        bool ValidateImageFile(IFormFile file);
        //Task<ServiceResponse> ScanFileWithDefenderAsync(IFormFile file);
        Task<ServiceResponse> ValidateImageAsync(IFormFile imageFile, long maxFileSize);
    }

}
