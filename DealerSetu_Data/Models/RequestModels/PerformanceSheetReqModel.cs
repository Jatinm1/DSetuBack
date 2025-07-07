using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class PerformanceSheetReqModel
    {
        public int DealerEmpId { get; set; }
        public int Month { get; set; }
        public required string FYear { get; set; }
    }
    public class PdfUploadModel
    {
        public string Base64Data { get; set; } = string.Empty;
        public string FileName { get; set; } = "document.pdf";
    }
}
