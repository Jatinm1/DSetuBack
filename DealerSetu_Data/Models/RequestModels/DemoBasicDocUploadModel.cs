using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class DemoBasicDocUploadModel
    {
        //public int Id { get; set; }
        public int RequestId { get; set; }
        //public string EmpNo { get; set; }
        //public string RequestNo { get; set; }
        //public string DealerNo { get; set; }
        //public string DealerLocation { get; set; }
        public string Model { get; set; }
        public string ChassisNo { get; set; }
        public string EngineNo { get; set; }
        public string DateOfBilling { get; set; }
        public IFormFile? InvoiceFile { get; set; }
        public IFormFile? RCFile { get; set; }
        public IFormFile? InsuranceFile { get; set; }
        //public IFormFile FileSale { get; set; }
        //public IFormFile FileTractor { get; set; }
        //public IFormFile FilePicture { get; set; }
        //public IFormFile FilePicTractor { get; set; }
        //public IFormFile LogDemonsFile { get; set; }
        //public IFormFile Affidavitfile { get; set; }
        //public IFormFile SaleDeedfile { get; set; }
        //public string FileSaleExt { get; set; }
        //public string FileTractorExt { get; set; }
        //public string FilePictureExt { get; set; }
        //public string FilePicTractorExt { get; set; }
        //public string InvoiceFileExt { get; set; }
        //public string RCFileExt { get; set; }
        //public string InsuranceFileExt { get; set; }
        //public string LogDemonsExt { get; set; }
        //public string AffidavitExt { get; set; }
        //public string saledeedExt { get; set; }
        //public string CreatedDate { get; set; }
        //public string CreatedBy { get; set; }
        //public string Status { get; set; }

    }
}
