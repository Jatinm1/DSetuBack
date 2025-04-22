using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class StateModel
    {
        public int StateId { get; set; }
        public string State { get; set; }
    }

    public class DealerStateModel
    {
        public string DealerState { get; set; }
    }

    public class FYearModel
    {
        public string FYID { get; set; }
        public string FYValue { get; set; }
    }

}
