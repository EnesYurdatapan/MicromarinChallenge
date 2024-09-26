using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTOs
{
    public class FilteredObjectDTO
    {
        public string ObjectType { get; set; }  // Product, Order vs.
        public dynamic Filters { get; set; }    // Dinamik filtreler (JSON formatında)
    }
}
