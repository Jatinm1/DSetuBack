using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public class UpdateResult
    {
        public int RowsAffected { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}
