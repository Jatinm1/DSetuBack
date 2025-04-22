using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public class AdditionResponse<T>
    {
        public bool IsError { get; set; }
        public string Object { get; set; }
        public string Message { get; set; }
        public T Result { get; set; }
    }

}
