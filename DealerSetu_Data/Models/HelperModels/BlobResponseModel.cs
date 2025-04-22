using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public class BlobReponseModel
    {
        public string Status { get; set; } = "Success";
        public string Code { get; set; } = "200";
        public bool isError { get; set; } = false;
        public string Error { get; set; }
        public object Data { get; set; }

        public void SetSuccess(string message = null)
        {
            Status = "Success";
            Code = "200";
            isError = false;
            Error = null;
            Data = message;
        }

        public void SetFailure(string errorMessage)
        {
            Status = "Failure";
            Code = "400";
            isError = true;
            Error = errorMessage;
            Data = null;
        }

        public void SetServerError(string errorMessage)
        {
            Status = "Error";
            Code = "500";
            isError = true;
            Error = errorMessage;
            Data = null;
        }
    }
}
