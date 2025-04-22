using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class EmployeeListResult
    {
        public List<UserModel> Employees { get; set; }
        public int TotalCount { get; set; }
    }

    public class DealerListResult
    {
        public List<DealerModel> Dealers { get; set; }
        public int TotalCount { get; set; }
    }

    public class RequestListResult
    {
        public List<DealerModel> Reports { get; set; }
        public int TotalCount { get; set; }
    }

    public class DemoReqListResult
    {
        public List<DemoReqModel> DemoReqList { get; set; }
        public int TotalCount { get; set; }
    }
}
