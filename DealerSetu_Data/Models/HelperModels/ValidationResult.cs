using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public class ValidationResult
    {
        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }

        private ValidationResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static ValidationResult Success() => new(true, string.Empty);
        public static ValidationResult Failure(string message) => new(false, message);
    }

    public class DefenderScanResult
    {
        public bool IsClean { get; set; }
        public string InfectedFiles { get; set; }
        public string Message { get; set; }
    }
}
