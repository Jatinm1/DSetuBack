using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class DemoListModel
    {
        public string RequestNo { get; set; }

        public DateTime CreatedOn { get; set; }
        public string CreatedDate { get; set; }
        public DateTime TMDate { get; set; }
        public string TMDateResponse { get; set; }

        public DateTime SHDate { get; set; }
        public string SHDateResponse { get; set; }

        public DateTime HODate { get; set; }
        public string HODateResponse { get; set; }
        public int DemoReqid { get; set; }


    }
}
