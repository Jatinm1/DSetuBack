using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class DemoTractorResponseModel
    {
        public int Id { get; set; }
        public int DemoRequestId { get; set; }
        public string RequestNo { get; set; }
        public string DealerNo { get; set; }
        public string DealerLocation { get; set; }
        public string ModelRequested { get; set; }
        public string Reason { get; set; }
        public string SchemeType { get; set; }
        public string SpecialVariant { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedDateString { get; set; }
        public string CreatedBy { get; set; }
        public bool IsRejected { get; set; }
        public string RejectedBy { get; set; }
        public string RejectedRemarks { get; set; }
        public string Remarks { get; set; }
        public string DealerState { get; set; }
        public string Model { get; set; }
        public string chasisno { get; set; }
        public string engineno { get; set; }
        public DateTime dateofbilling { get; set; }
        public string dateofbillingstring { get; set; }
        public string InvoiceFile { get; set; }
        public string RCFile { get; set; }
        public string FileSale { get; set; }


        public string FileTractor { get; set; }

        public string FilePicture { get; set; }

        public string FilePicTractor { get; set; }

        public string InsuranceFile { get; set; }
        public string LogDemons { get; set; }
        public string Affidavit { get; set; }
        public string saledeed { get; set; }
        public string Message { get; set; }
        public int HpCategoryId { get; set; }
        public int Implementrequired { get; set; }
        public int ImplementId { get; set; }


    }
}
