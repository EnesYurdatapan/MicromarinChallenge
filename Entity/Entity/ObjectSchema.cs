using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class ObjectSchema:BaseEntity
    {
        public string ObjectType { get; set; }
        public dynamic Schema { get; set; }
    }
}
