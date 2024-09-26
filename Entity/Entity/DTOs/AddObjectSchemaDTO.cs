using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTOs
{
    public class AddObjectSchemaDTO
    {
        public string ObjectType { get; set; }
        public dynamic Schema { get; set; }
    }
}
