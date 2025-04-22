using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class ReportModel
    {
        public string RequestNo { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedDate { get; set; }

        public DateTime? ResponseOn { get; set; }
        public string ResponseDate { get; set; }


    }
}
